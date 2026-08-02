const string ProcessName = "Starfall.Client";

if (args.Length != 0)
{
    Console.Error.WriteLine($"{ProcessName} foundation shell does not accept arguments.");
    return 2;
}

Console.WriteLine($"{ProcessName} foundation shell started.");
return 0;
