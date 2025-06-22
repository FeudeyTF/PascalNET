namespace PascalNET
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Улучшенная система грамматического анализа программ Pascal");
            Console.WriteLine(new string('=', 70));

            List<CodeExample> examples =
            [
                new CodeExample("Корректная программа",
                   @"var x, y, z: integer;
                    begin
                        x := 10;
                        y := x + 5;
                        if x > y then
                            x := x * 2
                        else
                            y := y + 1
                    end."
                ),
                new CodeExample("Программа с лексическими ошибками",
                    @"var x: integer;
                    begin
                        x := 10@;
                        y := x + ""незакрытая строка;
                    end."
                ),
                new CodeExample("Программа с синтаксическими ошибками",
                    @"var x: integer
                        y: real;
                    begin
                        x := 10;
                        z := x + y;
                        if x > then
                            x := w * 2;
                    end."
                ),                new CodeExample("Программа с семантическими ошибками",
                    @"var x: integer;
                        y: real;
                        z: boolean;
                    begin
                        x := 10;
                        y := x + 5.5;
                        z := x + y;
                        w := x * 2;
                        x := ""строка""
                    end."
                ),
                new CodeExample("Комплексная программа с разными типами ошибок",
                    @"var a, b: integer;
                        c: unknown_type;
                    begin
                        a := 10;
                        b := a +;
                        c := a and b;
                        if a = then
                            a := undeclared_var
                        else begin
                            b := a / 0;
                            a := b + ""string""
                        end
                    end."
                )
            ];

            IDE compiler = new();

            for (int i = 0; i < examples.Count; i++)
            {
                Console.WriteLine();
                Console.WriteLine($"=== Пример {i + 1}: {examples[i].Name} ===");
                Console.WriteLine("Исходный код:");
                Console.WriteLine(examples[i].SourceCode);
                Console.WriteLine();
                compiler.Compile(examples[i].SourceCode);
                var report = compiler.GetCompilationReport(examples[i].SourceCode);
                report.PrintSummary();

                Console.WriteLine(new string('-', 70));
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private class CodeExample
        {
            public string Name { get; set; }
            public string SourceCode { get; set; }

            public CodeExample(string name, string sourceCode)
            {
                Name = name;
                SourceCode = sourceCode;
            }
        }
    }
}
