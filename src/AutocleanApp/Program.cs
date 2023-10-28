using AutocleanRegistry;
Console.ForegroundColor = ConsoleColor.White;

var rc = new RegistryController();
rc.AutocleanUninstall();

Console.ForegroundColor= ConsoleColor.Yellow;
Console.WriteLine("Press any key to exit");
Console.ForegroundColor = ConsoleColor.White;
Console.ReadKey();