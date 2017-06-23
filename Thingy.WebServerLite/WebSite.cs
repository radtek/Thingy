﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Thingy.WebServerLite.Api;

namespace Thingy.WebServerLite
{
    public class WebSite : IWebSite
    {
        private readonly IControllerProvider controllerProvider;
        private readonly string name;
        private readonly string path;

        public WebSite(IViewProvider viewProvider, IControllerProviderFactory controllerProviderFactory, IController[] controllers, string name, int portNumber, string path)
        {
            this.name = name;
            this.PortNumber = portNumber;
            this.path = path;
            this.controllerProvider = controllerProviderFactory.Create(controllers.Where(c => c.GetType().Assembly.GetName().Name.EndsWith(name)).ToArray());
            this.ViewProvider = viewProvider;
            IsDefault = false;
            Priority = Priorities.Normal;
        }

        public bool IsDefault { get; set; }

        public int PortNumber { get; private set; }

        public Priorities Priority { get; set; }

        public IViewProvider ViewProvider { get; private set; }

        public bool CanHandle(IWebServerRequest request)
        {
            return (IsDefault || name == request.WebSiteName);
        }

        public void Handle(IWebServerRequest request, IWebServerResponse response)
        {
            request.WebSite = this;

            IController controller = controllerProvider.GetControllerForRequest(request);

            if (controller != null)
            {
                controller.Handle(request, response);
            }
            else
            {
                string filePath = Path.Combine(path, request.FilePath);

                if (File.Exists(filePath))
                {
                    response.FromFile(filePath);
                }
                else
                {
                    response.NotFound(request);
                }
            }
        }
    }
}