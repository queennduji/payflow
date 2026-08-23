using System.Reflection;
using Polly;

var pollyCore = Assembly.Load("Polly.Core");
var t1 = pollyCore.GetType("Polly.RetryResiliencePipelineBuilderExtensions")!;
foreach (var m in t1.GetMethods(BindingFlags.Public | BindingFlags.Static))
    Console.WriteLine(m);
var t2 = pollyCore.GetType("Polly.CircuitBreakerResiliencePipelineBuilderExtensions")!;
foreach (var m in t2.GetMethods(BindingFlags.Public | BindingFlags.Static))
    Console.WriteLine(m);
var t3 = pollyCore.GetType("Polly.TimeoutResiliencePipelineBuilderExtensions")!;
foreach (var m in t3.GetMethods(BindingFlags.Public | BindingFlags.Static))
    Console.WriteLine(m);
Console.WriteLine("--- PredicateBuilder ---");
var pb = pollyCore.GetType("Polly.PredicateBuilder");
Console.WriteLine(pb);
foreach (var m in pb!.GetMethods()) Console.WriteLine("  " + m);
