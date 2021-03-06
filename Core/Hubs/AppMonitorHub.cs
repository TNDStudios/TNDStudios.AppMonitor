﻿using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TNDStudios.AppMonitor.Objects;

namespace TNDStudios.AppMonitor.Core
{
    public class AppMonitorHub : Hub
    {
        /// <summary>
        /// INjected core service from the Singleton provided at setup
        /// </summary>
        private readonly IAppMonitorCoordinator coordinator;

        /// <summary>
        /// Default constructor with the App Monitor Core injected into it
        /// </summary>
        public AppMonitorHub(IAppMonitorCoordinator coordinator)
        {
            this.coordinator = coordinator;
            this.coordinator.RegisteredHub = this; // Register this hub with the coordinator
        }

        /// <summary>
        /// A metric was received from an application
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="path">The path for the metric seperated with pipes</param>
        /// <param name="metric">The value of the metric</param>
        /// <returns></returns>
        public async Task SendMetric(string applicationName, string path, Double metric)
        {
            ReportingApplication application = coordinator.GetApplication(applicationName);

            // Ask the application to add the metric and handle any locking internally
            application.AddMetric(path, metric);

            // Tell all the clients a new metric was received
            await Clients.All.SendAsync("ReceiveMetric", applicationName, path, metric);
        }

        /// <summary>
        /// An error was recieved from an application via SignalR
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="errorMessage">The error message as generated by the application</param>
        /// <returns></returns>
        public async Task SendError(string applicationName, string errorMessage)
        {
            // Get the application from the coordinator
            ReportingApplication application = coordinator.GetApplication(applicationName);

            // Add the error to the memory error list for this application
            application.Errors.Add(new ReportingError() { Message = errorMessage });
            application.AddMetric("Errors", 1); // Add an error to the default error metric

            // Tell all the clients a new error was received
            await Clients.All.SendAsync("ReceiveError", applicationName, errorMessage);
        }
    }
}
