﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thingy.WebServerLite.Api
{
    public abstract class ControllerBase : IController
    {
        public ControllerBase()
        {
            Priority = Priorities.Normal;
        }

        public Priorities Priority { get; set; }

        public bool CanHandle(IWebServerRequest request)
        {
            return (GetType().Name.StartsWith(request.ControllerName));
        }

        public void Handle(IWebServerRequest request, IWebServerResponse response)
        {
            MethodInfo method = GetType().GetMethods(BindingFlags.Public).FirstOrDefault(m => m.Name == request.ControllerMethodName && SupportsHttpMethod(m, request.HttpMethod));

            if (method != null)
            {
                if (UserAuthorized(method, request))
                {
                    try
                    {
                        object[] parameters = GetParametersFromRequest(method, request);
                        object o = method.Invoke(this, parameters);
                        IViewResult result = request.WebSite.ViewProvider.GetViewForRequest(request).Render(o);
                        response.FromString(result.Content, result.ContentType);
                    }
                    catch(Exception e)
                    {
                        response.InternalError(request, e);
                    }
                }
                else
                {
                    response.NotAllowed(request);
                }
            }
            else
            {
                response.NotFound(request);
            }
        }

        private object[] GetParametersFromRequest(MethodInfo method, IWebServerRequest request)
        {
            if (request.UrlValues.Any())
            {
                return BindUrlValuesByPosition(method, request.UrlValues);
            }
            else
            {
                return BindFieldsByName(method, request);
            }
        }

        private object[] BindUrlValuesByPosition(MethodInfo method, string[] values)
        {
            object[] parameters = new object[values.Length];
            int index = 0;

            foreach(ParameterInfo parameterInfo in method.GetParameters())
            {
                parameters[index] = StringToObject(values[index], parameterInfo.ParameterType);
                index++;
            }

            return parameters;
        }

        private object[] BindFieldsByName(MethodInfo method, IWebServerRequest request)
        {
            ParameterInfo[] parameterInfos = method.GetParameters();
            object[] parameters = new object[parameterInfos.Length];
            int index = 0;

            foreach (ParameterInfo parameterInfo in method.GetParameters())
            {
                parameters[index] = BindParameterByName(parameterInfo.ParameterType, parameterInfo.Name, request, string.Empty);
                index++;
            }

            return parameters;
        }

        private object BindParameterByName(Type bindType, string bindName, IWebServerRequest request, string prefix)
        {
            if (bindType.IsPrimitive || bindType == typeof(string))
            {
                string name = string.Format("{0}{1}", prefix, bindName);

                if (request.Fields.ContainsKey(name))
                {
                    return StringToObject(request.Fields[name], bindType);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                object o = Activator.CreateInstance(bindType); // If this is too limiting then could we use the CastleContainer??

                foreach (PropertyInfo propertyInfo in bindType.GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    object value = BindParameterByName(propertyInfo.PropertyType, propertyInfo.Name, request, string.Format("{0}{1}.", prefix, bindName));

                    if (value != null)
                    {
                        propertyInfo.SetValue(o, value);
                    }
                }

                return o;
            }
        }

        private object StringToObject(string value, Type type)
        {
            if (type == typeof(string))
            {
                return value;
            }

            if (type == typeof(int))
            {
                return Convert.ToUInt32(value);
            }

            if (type == typeof(DateTime))
            {
                return Convert.ToDateTime(value);
            }

            if (type == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }

            if (type == typeof(long))
            {
                return Convert.ToInt64(value);
            }

            if (type == typeof(double))
            {
                return Convert.ToDouble(value);
            }

            if (type == typeof(byte))
            {
                return Convert.ToByte(value);
            }

            if (type == typeof(sbyte))
            {
                return Convert.ToSByte(value);
            }

            if (type == typeof(short))
            {
                return Convert.ToInt16(value);
            }

            if (type == typeof(ushort))
            {
                return Convert.ToUInt16(value);
            }

            if (type == typeof(uint))
            {
                return Convert.ToUInt32(value);
            }

            if (type == typeof(ulong))
            {
                return Convert.ToUInt64(value);
            }

            if (type == typeof(float))
            {
                return Convert.ToSingle(value);
            }

            if (type == typeof(decimal))
            {
                return Convert.ToDecimal(value);
            }

            if (type == typeof(char))
            {
                return Convert.ToChar(value);
            }

            throw new ArgumentException(string.Format("Unknown Primitive {0}, value \"{1}\"", type, value));
        }

        private bool UserAuthorized(MethodInfo method, IWebServerRequest request)
        {
            IList<AuthorizedRoleAttribute> attributes = method.GetCustomAttributes<AuthorizedRoleAttribute>().ToList();

            return !attributes.Any() || attributes.Any(a => request.User.Roles.Contains(a.Role));
        }

        private bool SupportsHttpMethod(MethodInfo m, string httpMethod)
        {
            return m.GetCustomAttributes<HttpMethodAttribute>().Any(a => a.Supports == httpMethod);
        }
    }
}