using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace aspnettest
{

    public interface IToggledSingleton 
    {
        bool GetFeatureValue();
    }

    public class ToggledSingleton : IToggledSingleton
    {
        private readonly HubConnection _connection;

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        private string GetClientUrl(string endpoint, string hubName)
        {
            return $"{endpoint}/client/?hub={hubName}";
        }

        private bool _featureValue;

        public ToggledSingleton()
        {
            var connectionString = "MyConnectionString";
            var hubName = "MyHubName";
            var userId = "MyUserId";
            
            Console.WriteLine("ToggledSingleton public constructor has been invoked");

            var serviceUtils = new ServiceUtils(connectionString);

            var url = GetClientUrl(serviceUtils.Endpoint, hubName);

            _connection = new HubConnectionBuilder()
                .WithUrl(url, option =>
                {
                    option.AccessTokenProvider = () =>
                    {
                        return Task.FromResult(serviceUtils.GenerateAccessToken(url, userId));
                    };
                }).Build();

            _connection.Closed += async (error) =>
            {
                Console.WriteLine("Connection is borked. Trying to reconnect...");
                await Task.Delay(new Random().Next(0,5) * 1000);
                await _connection.StartAsync();
            };

            _connection.On<string, string>("SendMessage",
                (string server, string message) =>
                {
                    _featureValue = !_featureValue;
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Received message from server {server}: {message}");
                });

            StartListeningForEvents().GetAwaiter().GetResult();
        }

        private async Task StartListeningForEvents()
        {
            Console.WriteLine("Anchors away");
            await _connection.StartAsync();
        }

        public bool GetFeatureValue() => _featureValue;
    }

}