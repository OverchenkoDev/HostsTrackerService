using HostTracker.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EventLog = HostTracker.Models.EventLog;

namespace HostTracker
{
    public class TrackerService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory scopeFactory;
        private Timer _maintimer;
        private static readonly HttpClient client = new HttpClient();

        public TrackerService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _maintimer = new Timer(mainTimerTick, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void mainTimerTick(object state)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                HostTrackerContext db = new HostTrackerContext();
                Hosts[] servicesToPing = new Hosts[0];
                List<EventLog> events = new List<EventLog>();
                try
                {
                    db = scope.ServiceProvider.GetRequiredService<HostTrackerContext>();
                    servicesToPing = db.Hosts.ToArray();
                }
                catch (Exception ex)
                {
                    EventLog error = new EventLog();
                    error.Message = ex.Message;
                    error.Details = ex.StackTrace;
                    error.Type = "Ошибка во время подключения к БД";
                    error.DateTime = DateTime.UtcNow;
                    db.EventLog.Add(error);
                    db.SaveChanges();
                }
                if (servicesToPing.Count() > 0)
                {
                    foreach (Hosts host in servicesToPing)
                    {
                        try
                        {
                            string domain = host.CheckDomain;
                            if (!domain.Contains("https://") || !domain.Contains("http://"))
                                domain = $@"http://{domain}";
                            var checkingResponse = await CheckHost(domain);
                            if (!checkingResponse.IsSuccessStatusCode)
                            {
                                if (domain.Contains("azurewebsites.net"))
                                {
                                    if (checkingResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                                    {
                                        Dictionary<string, string> data = new Dictionary<string, string>();
                                        data.Add("text", $@"Недоступен сайт {host.ServiceName} по адресу {host.CheckDomain}");
                                        FormUrlEncodedContent content = new FormUrlEncodedContent(data);
                                        await client.PostAsync(host.NotificationUrl, content);
                                    }
                                }
                                else
                                {
                                    Dictionary<string, string> data = new Dictionary<string, string>();
                                    data.Add("text", $@"Недоступен сайт {host.ServiceName} по адресу {host.CheckDomain}");
                                    FormUrlEncodedContent content = new FormUrlEncodedContent(data);
                                    await client.PostAsync(host.NotificationUrl, content);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                Dictionary<string, string> data = new Dictionary<string, string>();
                                data.Add("text", $@"Недоступен сайт {host.ServiceName} по адресу {host.CheckDomain}");
                                FormUrlEncodedContent content = new FormUrlEncodedContent(data);
                                await client.PostAsync(host.NotificationUrl, content);
                                EventLog error = new EventLog();
                                error.Message = ex.Message;
                                error.Details = ex.StackTrace;
                                error.Type = $@"Ошибка во время проверки сервиса {host.ServiceName}";
                                error.DateTime = DateTime.UtcNow;
                                events.Add(error);
                            }
                            catch (Exception exc)
                            {
                                EventLog error = new EventLog();
                                error.Message = exc.Message;
                                error.Details = exc.StackTrace;
                                error.Type = $@"Ошибка во время отправки сообщения об ошибке по адресу {host.NotificationUrl} во время проверки сервиса {host.ServiceName}";
                                error.DateTime = DateTime.UtcNow;
                                events.Add(error);
                            }
                        }
                    }
                    db.EventLog.AddRange(events);
                    db.SaveChanges();
                }
            }
        }

        async Task<HttpResponseMessage> CheckHost(string host)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            return await client.GetAsync(host);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _maintimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _maintimer?.Dispose();
        }
    }
}