using CampusEatsv2.Notification.Consumers;
using CampusEatsv2.Notification.Messaging;
using CampusEatsv2.Notification.Services;

var builder = Host.CreateApplicationBuilder(args);
// Add the stmp email sender into container
builder.Services.AddSingleton<SmtpEmailSender>();

// Add consumer into campuseats-notification container
builder.Services.AddScoped<CustomerRegisteredConsumer>();
builder.Services.AddScoped<OrderDeliveredConsumer>();
builder.Services.AddScoped<OrderPickedUpConsumer>();
builder.Services.AddScoped<CourierApprovedConsumer>();
builder.Services.AddScoped<CourierDeclinedConsumer>();

// Add worker that performs actions
builder.Services.AddHostedService<RabbitMqWorker>();

var app = builder.Build();
app.Run();