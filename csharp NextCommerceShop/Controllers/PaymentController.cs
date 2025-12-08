// inside Callback():
// Use the provider prefix that matches the provider class name (StubProvider -> "Stub")
var result = await _paymentService.VerifyAsync("Stub", parameters);