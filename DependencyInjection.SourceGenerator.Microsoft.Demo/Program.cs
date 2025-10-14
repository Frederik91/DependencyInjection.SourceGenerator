// See https://aka.ms/new-console-template for more information
using DependencyInjection.SourceGenerator.Microsoft.Demo;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddDependencyInjectionSourceGeneratorMicrosoftDemo();
var provider = services.BuildServiceProvider();

var factory = provider.GetRequiredService<ITestServiceFactory>();
var testService = factory.Create(42);
Console.WriteLine(testService.Value);
