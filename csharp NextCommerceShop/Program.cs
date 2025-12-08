// register the concrete provider(s)
builder.Services.AddSingleton<IPaymentProvider, StubProvider>();