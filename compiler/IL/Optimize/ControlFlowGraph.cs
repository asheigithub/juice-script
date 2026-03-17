using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace juicescript.compiler.IL.Optimize
{
    public class ControlFlowGraph
    {
        private static int _printCounter = 0;

        public ASMethod Method { get; set; }
        public List<BasicBlock> Blocks { get; set; }
        
       
        public ControlFlowGraph(ASMethod method)
        {
            Method = method;
            Blocks = new List<BasicBlock>();
            
            
        }

        public Instruction[] FlattenInstructions()
        {
            var sortedBlocks = Blocks.OrderBy(b => b.OriginalIndex).ToList();
            var result = new List<Instruction>();
            foreach (var block in sortedBlocks)
            {
                result.AddRange(block.Instructions);
            }
            return result.ToArray();
        }

        public void Print()
        {
            _printCounter++;
            string outputDir = @"G:\temp";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string methodName = string.IsNullOrEmpty(Method?.Name) ? $"method_{_printCounter}" : SanitizeFileName(Method.Name);
            string filePath = Path.Combine(outputDir, $"cfg_{methodName}_{_printCounter}.html");

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"utf-8\">");
            sb.AppendLine($"    <title>Control Flow Graph - {methodName}</title>");
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine("        h1 { color: #333; }");
            sb.AppendLine("        .info { background: white; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
            sb.AppendLine("        .mermaid { background: white; padding: 20px; border-radius: 5px; }");
            sb.AppendLine("        table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
            sb.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("        th { background: #4CAF50; color: white; }");
            sb.AppendLine("        tr:nth-child(even) { background: #f2f2f2; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <h1>Control Flow Graph</h1>");
            sb.AppendLine($"    <div class=\"info\">");
            sb.AppendLine($"        <p><strong>Method:</strong> {Method?.Name ?? "Unknown"}</p>");
            sb.AppendLine($"        <p><strong>Total Blocks:</strong> {Blocks.Count}</p>");
            sb.AppendLine($"    </div>");

            // Mermaid diagram
            sb.AppendLine("    <div class=\"mermaid\">");
            sb.AppendLine("    flowchart LR");

            foreach (var block in Blocks)
            {
                string label = GetBlockLabel(block);
                sb.AppendLine($"        BB{block.BlockId}[\"{label}\"]");
            }

            foreach (var block in Blocks)
            {
                foreach (var succ in block.Successors)
                {
                    string edgeLabel = "";
                    if (block.JumpTargetFlagId.HasValue && succ == block.Successors.FirstOrDefault())
                    {
                        edgeLabel = "|jmp|";
                    }
                    else if (block.HasFallThrough && succ == block.Successors.LastOrDefault())
                    {
                        edgeLabel = "|ft|";
                    }
                    sb.AppendLine($"        BB{block.BlockId} -->{edgeLabel} BB{succ.BlockId}");
                }
            }

            sb.AppendLine("    </div>");

            // Block details table
            sb.AppendLine("    <h2>Block Details</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("        <tr><th>Block</th><th>Range</th><th>Predecessors</th><th>Successors</th><th>Instructions</th></tr>");

            foreach (var block in Blocks)
            {
                string preds = block.Predecessors.Count == 0 ? "(none)" : string.Join(", ", block.Predecessors.Select(p => $"BB{p.BlockId}"));
                string succs = block.Successors.Count == 0 ? "(none)" : string.Join(", ", block.Successors.Select(s => $"BB{s.BlockId}"));
                string instructions = string.Join("<br>", block.Instructions.Select((ins, idx) => $"{block.StartIndex + idx}: {ins}"));
                //if (block.Instructions.Count > 10)
                //    instructions += "<br>...";

                sb.AppendLine($"        <tr>");
                sb.AppendLine($"            <td>BB{block.BlockId}</td>");
                sb.AppendLine($"            <td>{block.StartIndex}-{block.EndIndex}</td>");
                sb.AppendLine($"            <td>{preds}</td>");
                sb.AppendLine($"            <td>{succs}</td>");
                sb.AppendLine($"            <td>{instructions}</td>");
                sb.AppendLine($"        </tr>");
            }

            sb.AppendLine("    </table>");

           
            sb.AppendLine("    <script>");
            sb.AppendLine("        mermaid.initialize({ startOnLoad: true });");
            sb.AppendLine("    </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"CFG HTML generated: {filePath}");
        }

        private string GetBlockLabel(BasicBlock block)
        {
            var sb = new StringBuilder();
            sb.Append($"BB{block.BlockId}\\n");

            if (block.Instructions.Count > 1)
            {
				sb.Append(block.Instructions[0].INS_Code.ToString() + "\\n");
			}
            if (block.Instructions.Count > 2)
            {
				sb.Append("...\\n");
			}
			if (block.Instructions.Count > 0)
            {
                var lastIns = block.Instructions[block.Instructions.Count - 1];
                sb.Append(lastIns.INS_Code.ToString());
            }

            return sb.ToString();
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

    }
}
