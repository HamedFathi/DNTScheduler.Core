﻿using System;
using DNTScheduler.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DNTScheduler.Core
{
    /// <summary>
    ///  DNTScheduler ServiceCollection Extensions
    /// </summary>
    public static class SchedulerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds default DNTScheduler providers.
        /// </summary>
        public static void AddDNTScheduler(this IServiceCollection services, Action<ScheduledTasksStorage> options)
        {
            services.TryAddSingleton<IJobsRunnerTimer, JobsRunnerTimer>();
            services.TryAddSingleton<IScheduledTasksCoordinator, ScheduledTasksCoordinator>();

            configTasks(services, options);
        }

        private static void configTasks(IServiceCollection services, Action<ScheduledTasksStorage> options)
        {
            var storage = new ScheduledTasksStorage();
            options(storage);
            registerTasks(services, storage);
            addPingTask(services, storage);
            services.TryAddSingleton(Options.Create(storage));
        }

        private static void registerTasks(IServiceCollection services, ScheduledTasksStorage storage)
        {
            foreach (var task in storage.Tasks)
            {
                services.TryAddTransient(task.TaskType);
            }
        }

        private static void addPingTask(IServiceCollection services, ScheduledTasksStorage storage)
        {
            if (string.IsNullOrWhiteSpace(storage.SiteRootUrl))
            {
                return;
            }

            services.AddHttpClient<MySitePingClient>(client =>
            {
                client.BaseAddress = new Uri(storage.SiteRootUrl);
                client.DefaultRequestHeaders.ConnectionClose = true;
                client.DefaultRequestHeaders.Add("User-Agent", "DNTScheduler 1.0");
            });

            storage.AddScheduledTask<PingTask>(runAt: utcNow => utcNow.Second == 1);
            services.TryAddSingleton<PingTask>();
        }
    }
}