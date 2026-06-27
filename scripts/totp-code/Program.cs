using OtpNet;

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run --project scripts/totp-code -- <base32-secret>");
    return 1;
}

var bytes = Base32Encoding.ToBytes(args[0]);
var totp = new Totp(bytes);
Console.Write(totp.ComputeTotp());
return 0;
