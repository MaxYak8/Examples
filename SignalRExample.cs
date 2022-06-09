// from Startup.cs

services.AddSignalR(hubOptions => {
                hubOptions.EnableDetailedErrors = true;
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
            });
			
services.AddSingleton<SomeHub>();

app.UseEndpoints(endpoints => {
                endpoints.MapHub<SomeHub>("/somehub").RequireAuthorization();
}

// from SomeHub
public class SomeHub : Microsoft.AspNetCore.SignalR.Hub {
        public string ConnectionId() => Context.ConnectionId;
        public void Callback(SomeDto message) {
            Response?.Invoke(message);
        }

        public static event Action<SomeDto> Response;
}
		
// from service on server side

// Service constructor

...
Service(IHubContext<ProjectNumberHub> hubContext) {
            _hubContext = hubContext;
        }
...

// Method in service

await _hubContext.Clients.All.SendAsync("SomeRequest", data);

            var are = new AutoResetEvent(false);
            someDto responseResult = null;
            void Callback(someDto message) {
                responseResult = message;
                are.Set();
            }

            SomeHub.Response += Callback;

            int waitTime = 0;
            while(responseResult == null && waitTime <= maxSecondsToResponse) {
                are.WaitOne(TimeSpan.FromSeconds(1));
                waitTime++;
            }

            SomeHub.Response -= Callback;
			
// use responseResult on server site next


// from service from client side

HubConnection connection = new HubConnectionBuilder()
                    .WithUrl(new Uri("http://localhost:5000/somehub"), options =>
                    {
                        options.Headers.Add("X-API-KEY", "Secret");
                    })
                    .WithAutomaticReconnect()
                    .Build();

                connection.On<SomeDto>("SomeRequest", (someDto) =>
                {
                    connection.InvokeAsync("Callback", new SomeDto
                    {
                        
                    });
                });

                connection.StartAsync();
				
				...
				
				conection.StopAsync();
}				
