
using juicescript.compiler;
using System.Runtime.InteropServices;
using System.Text;
using juicescript.compiler.parse;
using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils.HelpText;
using System.Linq;
using MyMD5;
using System.Collections;
using juicescript;
using System.Web;

namespace asc
{
    internal class Program
    {
        static int Main(string[] args)
        {
			try
			{
                var app = new CommandLineApplication
                {
                    Name = "asc",
                    Description = "juice-script-2 compiler ver:" + typeof(ScriptDefBuilder).Assembly.GetName().Version ,
                    //ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated
                };

                app.HelpOption(inherited: true).Description = "显示帮助信息";
                

                //var argumentDescription = app.Argument("Description", "The description of what you've done");

                var optionPackDir = app.Option("-r|--recurse <package>", "递归查找 .as3代码文件的目录，作为toplevel包所在目录。", CommandOptionType.MultipleValue)
                    //.IsRequired(false,"至少包含一个包路径。")
                    .Accepts(o => o.ExistingDirectory() )
                    ;

                var workDir = app.Option("-w|--workspace <dictionary>","编译器工作目录，默认为当前目录。", CommandOptionType.SingleValue)
                    .Accepts(o => o.ExistingDirectory())
                    ;
                workDir.DefaultValue = Directory.GetCurrentDirectory();

                var libs = app.Option("-l|--lib <file>", "要加载的.swc库文件", CommandOptionType.MultipleValue)
                    .Accepts(o => o.ExistingFile())
                    ;

                var force = app.Option("-f|--force", "强制重新编译字节码", CommandOptionType.NoValue);
                    
               

                var outSwcFile = app.Option("-o|--output <file>","输出swc文件名", CommandOptionType.SingleValue)
                    //.IsRequired(false,"需要指定输出文件名")
                    ;

				var cfgs = app.Option("-c|--controlflowgraph <method>", "显示指定method的控制流图", CommandOptionType.MultipleValue)
					;


				app.Command("list", listCommand =>
                {
                    listCommand.Description = "列出找到的代码文件。";    
                    //listCommand.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                    var optionListTags = listCommand.Option("-r|--recurse <package>", "递归查找 .as3代码文件的目录。", CommandOptionType.MultipleValue)
                        .Accepts(o => o.ExistingDirectory())
                    ;
                    
                    listCommand.OnExecute(() =>
                    {
                        int c = 0;
                        foreach (var pDir in optionListTags.Values)
                        {
                            var fs = System.IO.Directory.GetFiles(pDir,"*.as", SearchOption.AllDirectories);
                            foreach (var f in fs)
                            { 
                                Console.WriteLine(f);
                                c++;
                            }
                        }

                        Console.WriteLine($"total as3 files:{c}");

                        return 0;
                        //...
                    });

                });

                app.Command("ast" , (astCommand) =>
                {
                    astCommand.Description = @"解析 .as代码的抽象语法树。";
                    var asfile = astCommand.Argument("source file", ".as 代码文件")
                    .Accepts(o=>o.ExistingFile())
                    ;

                    var outfile = astCommand.Argument("output file", "将解析出的抽象语法树保存到文件中");


                    astCommand.OnExecute(
                        () =>
                        {
                            if (asfile.HasValue)
                            {
                                var lex = new Lex(null);
                                var tokens = lex.GetWords(AS3_LL1_GRAMMAR.GRAMMAR,false);
                                Parser parser = new Parser(tokens);
                                parser.ErrorOut = Console.Error;

                                ParseTree tree = parser.ParseTree(System.IO.File.ReadAllText(asfile.Value), AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS,asfile.Value);
                                if (parser.hasError)
                                {
                                    return 1;
                                }

                                var ast = new AS3AbstractSyntaxTree();
                                var as_srcfile = ast.Analyse(tree);

                                if (ast.SyntaxError != null)
                                {
                                    Console.Error.WriteLine(ast.SyntaxError);
                                    return 1;
                                }

                                StringBuilder sb = new StringBuilder();
                                as_srcfile.Write(sb);

                                Console.Write(sb.ToString());

                                if (outfile.HasValue)
                                { 
                                    System.IO.File.WriteAllText(outfile.Value, sb.ToString());
                                }
                            }
                            return 0;
                            
                        }
                        );

                }
                    );

                app.Command("symbol", (symbolCommand) => 
                {
                    symbolCommand.Description = "解析 .as代码中定义的符号。";
                    var dirList = symbolCommand.Option("-r|--recurse <package>", "递归查找 .as3代码文件的目录，作为toplevel包所在目录。", CommandOptionType.MultipleValue)
                    .IsRequired(false,"至少包含一个包路径。")
                    .Accepts(o => o.ExistingDirectory())
                    ;

                    var symWorkDir = symbolCommand.Option("-w|--workspace <dictionary>", "编译器工作目录，默认为当前目录。", CommandOptionType.SingleValue)
                    .Accepts(o => o.ExistingDirectory())
                    ;
                    symWorkDir.DefaultValue = Directory.GetCurrentDirectory();

                    var symVerbose = symbolCommand.Option("-v|--verbose", "显示解析出的信息", CommandOptionType.NoValue);


                    symbolCommand.OnExecute(
                        () => 
                        {
                            try
                            {
                                return ScriptDefBuilder.BuildDefines(dirList.Values.ToList(), symWorkDir.Value(), symVerbose.HasValue(),null,null,false,true );
                            }
                            catch (CompilerException ex)
                            {
                                try
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Error.WriteLine(ex.ToString());
                                }
                                finally
                                {
                                    Console.ResetColor();
                                }
                                return 1;
                            }
                            
                        }
                        );

                } );


                app.OnExecute(() =>
                {
                    if (optionPackDir.Values.Count == 0)
                    {
                        app.ShowHelp();
                        return 1;
                    }

                    try
                    {
                        string? out_swc = outSwcFile.Value();
                        if (out_swc == null)
                        {
                            out_swc = "o.swc";
                        }

                        if (!out_swc.EndsWith(".swc"))
                        {
                            out_swc += ".swc";
                        }

                        bool force_rebuild_bcode = force.HasValue() || cfgs.Values.Count>0 ;

                        
                        return new CompilePipeLine().Build( optionPackDir.Values.Select(p=>Path.GetFullPath(p)).ToList(),
                            Path.GetFullPath( workDir.Value()),
                            libs.Values.Distinct().Select( l=> System.IO.Path.GetFullPath(l) ).ToList(),
                            Path.GetFullPath(  System.IO.Path.Combine( workDir.Value(),out_swc) ),
                            force_rebuild_bcode, cfgs.Values.ToList()
							);
                    }
                    catch (CompilerException ex)
                    {
                        try
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine(ex.ToString());
                        }
                        finally
                        {
                            Console.ResetColor();
                        }
                        return 1;
                    }

                    
                });

                int code = app.Execute(args);

                return code;

            }
            catch (McMaster.Extensions.CommandLineUtils.CommandParsingException e)
            {
                Console.WriteLine(e.Message);
                return e.HResult;
            }
            










            //var lex = new Lex(null);


            ////var tokens = lex.GetWords("ab c \n var, a. \"bbc\" . * <a></a> 0xFF,45, /*注释注释*/ 5e8 ");

            //var tokens = lex.GetWords( AS3_LL1_GRAMMAR.GRAMMAR );

            //Parser parser = new Parser(tokens);




            //            var tree = parser.ParseTree(@" 

            //package 
            //{

            //   [Class];
            //   [Doc];
            //   public class B
            //   {
            //        [native,a];
            //        var BB = 1;

            //        const K = BB+3;

            //        [JJJ,1];
            //        public function CC(i = 99,j=( function(){} )()){
            //            1+1&9;

            //            var k = a::H.@id;

            //            namespace D = 'https://ns';

            //            switch(a)
            //            {
            //                case 1:
            //                    3+5;
            //                    break;
            //                default:
            //                case 1+1:
            //                {
            //                    if(3>2)
            //                        print(y);

            //                    throw e;
            //                }

            //            }



            //        } ;

            //        1+1;
            //   }
            //}

            //try
            //{
            //    a = b+c;
            //    a.bb();

            //    kll:with(a,b,c)
            //    {
            //        toString;
            //    }

            //}
            //catch(c)
            //{
            //    rty;
            //}
            //finally
            //{
            //    fin;
            //}


            //lbl:if(a+b,c+d)
            //{
            //   if(true)
            //        u=7+8?(t,y):(1,2,3);

            //    break;

            //    a:{
            //        if(false)
            //        {
            //            b:{
            //                5+7;
            //            }
            //        }


            //    }

            //}
            //else
            //{
            //    2+2;
            //    yield break;
            //    return (  3,4,5 );

            //    yield return (7,8,9);

            //    while(3+3)
            //    {
            //        if(3)
            //            return 1;
            //        else
            //            return 2;
            //    }

            //    do 
            //    {
            //	    while (1) 
            //	    {

            //	    }

            //    } while (5+6);



            //    ggg:for(var a = 99 in ( t,y,u, 5+[1, 2, 3]))
            //    {
            //        if(1)
            //        {}
            //        else
            //        	fff:for(var jjj=0;jjj<1;jjj--)
            //            {
            //                1+2+3+4;
            //            }   


            //    }

            //    for each (var b in new <int>[3,4,5]) 
            //    {
            //	    switch(b)
            //        {

            //        }
            //    }

            //}

            ////var a,b,c;
            ////interface iff
            ////{
            ////    static function JJ();
            ////}

            ////const j = a;

            ////var a=5;

            ////@id;

            ////@[3,4,5];

            ////a++.i;

            ////this.B..(j+1,i+1).(false);
            ////super.A::GG::KK( b::G );

            ////XML::b.(@number==1,@[7,8]);

            ////XML::aa.c::dd::ee.(b)::FF.@number == 100;

            ////B.(@number == 5  );


            ////new Vector.<int>(3,4,5);
            ////Vector.<int>([3,4,5]);
            ////new <int>[1,2,3];

            //// o = {
            ////    a:1,
            ////    b:{
            ////        c : ""g"",
            ////        d : 1?3:4
            ////    }
            ////    ,
            ////    c : trace(4,5,6)
            ////}


            ////new L();

            ////new K;

            ////new b(5,6 + a,7 , C(6));

            ////a(1,2).aabb;

            ////c.[3,1+3, (5), ( 1?3:(7,7) && 8), ""JJ""].length = 100 - 9[6];

            ////b = 1+1>2? (3? 4: 5 * 7) + 7  : (1,u); 

            ////a = 1+1 || b ?( a, 3 + 5):4 && ((a+ b >>> 3)) && 7;

            ////a + 1 = i + 3 && y && (b + 1 || 2 ) || 2 + 3 || (1 * 3 + 5);

            ////a = (a = b = 1 + 3,5+a, y*7 ) || 4 || (n  + 1,5) || 9 ;

            ////c || r = 1 + 1 || (u,2 - 2,(y,4-3),6 ) || y + h

            ////b = a && c= 1 + 1 && (y || (7,8));

            //",
            //AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS);
#if false

#if true
            //var tree = parser.ParseTree(System.IO.File.ReadAllText("F:\\GitHub\\juice-script\\testscript\\src\\Main.as"), AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS);
            var tree = parser.ParseTree(@"
            package
            {
               

                //[bcd(1)];
                //45;
                //[efg()];
                //[abc(1)];
                private class B implements A,B,C
                {                               
                     u function B()
                    {
                        use namespace t;
                        
                        trace(NI::[k]);

                        function J(){};
                        // var k;
                        //(function(){});
                    };
                    var K=5;


                    (function(){trace(""BCD"");})();

                    //native function KK(a:int);

                }


                (function abcd(){})();
                [LLM]
            }

            //class K
            //{
            //    ccdd var JJ;
            //    ccdd const UU;
            //}

            final class E
            {
             //use namespace NI;
            [Inspectable(category=""General"", enumeration=""off,on,auto"", defaultValue=""auto"")]
             NI var mm1 = 1;

            [Doc]
              var mm1 = 2;
             private var mm1 = 3;
                
                function get  A(i=1,j,...k,...l){}

            }
            //use namespace NI;
            var e = new E();
            trace( e.  public::mm1.internal::JJ ) ; 

            var f = (function(){});

var order = <order id=""i1000423"">
    <part id=""p343-3456"" quantity=""2""/>
</order> ;

trace(order.@id);

            ", AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS);


            //Console.WriteLine(tree.GetTreeString());

            if (parser.hasError)
            {
                Environment.ExitCode = 1;
            }
            else
            {
                var ast = new AS3AbstractSyntaxTree();
                var as3 = ast.Analyse(tree);

                if (ast.SyntaxError != null)
                {
                    Console.Error.WriteLine(ast.SyntaxError);


                    Environment.ExitCode = 1;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    as3.Write(sb);

                    Console.Write(sb.ToString());

                }
            }
#else
            var fs = System.IO.Directory.GetFiles("F:\\GitHub\\juice-script\\juice-script\\tests\\testscripts", "*.as", SearchOption.AllDirectories);
            foreach (var f in fs)
            {
                var tree = parser.ParseTree(System.IO.File.ReadAllText(f), AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS);
                if (parser.hasError)
                {
                    Console.WriteLine(tree.GetTreeString());
                    //Environment.ExitCode = 1;
                    Environment.Exit(1);
                }
                else
                {
                    var ast = new AS3AbstractSyntaxTree();
                    var as3 = ast.Analyse(tree);

                    if (ast.SyntaxError != null)
                    {
                        Console.Error.WriteLine(ast.SyntaxError);
                        Environment.ExitCode = 1;
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        as3.Write(sb);

                        Console.Write(sb.ToString());

                    }
                }


            }
#endif
            
#endif

            //Console.WriteLine(new MyMD5.MyMD5().Processing("hi"));
            //Console.WriteLine(new MyMD5.MyMD5().Hash("hi"));

        }


    }
}
