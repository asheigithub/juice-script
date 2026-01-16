using juicescript.compiler;
using juicescript.compiler.AST;
using juicescript.compiler.parse;

namespace compilerTests
{
    [TestClass]
    public class ScriptDefTest 
    {
        [TestMethod]
        public void TestDef()
        {
            var lex = new Lex(null);
            var tokens = lex.GetWords(AS3_LL1_GRAMMAR.GRAMMAR, false);
            Parser parser = new Parser(tokens);
            

            ScriptDefBuilder builder = new ScriptDefBuilder();
            AS3SrcFile as3;
            //{
            //    var script = builder.Build(parser,
            //        "F:\\GitHub\\juice-script\\testscript\\src\\pkg\\USEPKGC.as", "F:\\GitHub\\juice-script\\testscript\\src\\", out as3);

            //    string store;
            //    using (StringWriter writer = new StringWriter())
            //    {
            //        script.Serialize(writer);
            //        store = writer.ToString();
            //    }


            //    using (StringReader reader = new StringReader(store))
            //    {
            //        ScriptDef script1;
            //        var result = ScriptDef.Deserialize(reader, as3, out script1);

            //        Assert.AreEqual(result, ScriptDef.ReadResult.Success);


            //        using (StringWriter sw2 = new StringWriter())
            //        {
            //            script1.Serialize(sw2);
            //            Assert.AreEqual(store, sw2.ToString());
            //        }

            //    }
            //}

            {
                var fs = System.IO.Directory.GetFiles("F:\\GitHub\\juice-script-2\\juice-script-2\\fd_projs\\Zen3D-master\\src\\zen", "*.as", SearchOption.AllDirectories);
                foreach (var f in fs)
                {
                    var script = builder.Build(parser,
                        f, "F:\\GitHub\\juice-script-2\\juice-script-2\\fd_projs\\Zen3D-master\\src\\zen", out as3);
                    string store;
                    using (StringWriter writer = new StringWriter())
                    {
                        script.Serialize(writer);
                        store = writer.ToString();
                    }

                    using (StringReader reader = new StringReader(store))
                    {
                        ScriptDef script1;
                        var result = ScriptDef.Deserialize(reader, as3, out script1);
                        Assert.AreEqual(result, ScriptDef.ReadResult.Success);
                        using (StringWriter sw2 = new StringWriter())
                        {
                            script1.Serialize(sw2);
                            Assert.AreEqual(store, sw2.ToString());
                        }
                    }
                }
            }
            {
                var fs = System.IO.Directory.GetFiles("F:\\GitHub\\juice-script-2\\juice-script-2\\fd_projs\\juice_global\\src", "*.as", SearchOption.AllDirectories);
                foreach (var f in fs)
                {
                    string[] exclude =
                        { "Infinity.as", "isFinite.as",
                    "isNaN.as", "parseFloat.as", "parseInt.as",
                    "NaN.as","trace.as","undefined.as",
                    "flash\\utils\\getDefinitionByName.as",
                    "flash\\utils\\getQualifiedClassName.as"
                };

                    if (exclude.Contains(f.Replace("F:\\GitHub\\juice-script-2\\juice-script-2\\fd_projs\\juice_global\\src\\", "")))
                    {
                        continue;
                    }




                    var script = builder.Build(parser,
                        f, "F:\\GitHub\\juice-script-2\\juice-script-2\\fd_projs\\juice_global\\src", out as3);
                    string store;
                    using (StringWriter writer = new StringWriter())
                    {
                        script.Serialize(writer);
                        store = writer.ToString();
                    }

                    using (StringReader reader = new StringReader(store))
                    {
                        ScriptDef script1;
                        var result = ScriptDef.Deserialize(reader, as3, out script1);
                        Assert.AreEqual(result, ScriptDef.ReadResult.Success);
                        using (StringWriter sw2 = new StringWriter())
                        {
                            script1.Serialize(sw2);
                            Assert.AreEqual(store, sw2.ToString());
                        }
                    }
                }
            }
        }
    }
}
