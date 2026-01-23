using KeyRecorder.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "KeyRecorder Service";
});

builder.Services.AddHostedService<KeyRecorderWorker>();

var host = builder.Build();
host.Run();
