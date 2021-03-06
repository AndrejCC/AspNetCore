// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class HubEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
        public static IEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder routes, string pattern) where THub : Hub
        {
            return routes.MapHub<THub>(pattern, configureOptions: null);
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
        public static IEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder routes, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub
        {
            var marker = routes.ServiceProvider.GetService<SignalRMarkerService>();

            if (marker == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                                                    "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            var options = new HttpConnectionDispatcherOptions();
            // REVIEW: WE should consider removing this and instead just relying on the
            // AuthorizationMiddleware
            var attributes = typeof(THub).GetCustomAttributes(inherit: true);
            foreach (var attribute in attributes.OfType<AuthorizeAttribute>())
            {
                options.AuthorizationData.Add(attribute);
            }

            configureOptions?.Invoke(options);

            var conventionBuilder = routes.MapConnections(pattern, options, b =>
            {
                b.UseHub<THub>();
            });

            conventionBuilder.Add(e =>
            {
                // Add all attributes on the Hub has metadata (this will allow for things like)
                // auth attributes and cors attributes to work seamlessly
                foreach (var item in attributes)
                {
                    e.Metadata.Add(item);
                }

                // Add metadata that captures the hub type this endpoint is associated with
                e.Metadata.Add(new HubMetadata(typeof(THub)));
            });

            return conventionBuilder;
        }
    }
}
