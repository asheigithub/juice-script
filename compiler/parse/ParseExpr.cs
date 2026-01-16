using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace juicescript.compiler.parse
{
    /// <summary>
    /// 语法树节点
    /// </summary>
    public class ParseExpr
    {
        public ParseExpr Parent;
        public List<ParseExpr> Nodes = new List<ParseExpr>();

        public ParseNode GrammerLeftNode;
        public ParseLine SelectGrammerLine;
        public Token MatchedToken;
        public Token InputToken;

        internal string GetTreeString(int tabs, char tabchar)
        {
            StringBuilder sb = new StringBuilder();
            int tab_add;
            if (GrammerLeftNode.Type == ParseNodeType.non_terminal)
            {
                tab_add = 1;
                sb.Append(tabchar, tabs);
                if (SelectGrammerLine != null)
                {
                    sb.AppendLine(SelectGrammerLine.ToString() + tabchar + " [input \"" + MatchedToken.StringValue + "\"]");
                }
                else
                {
                    if (InputToken != null)
                    {
                        sb.AppendLine(GrammerLeftNode.Name + " ->***nochoose" + tabchar + " [input \"" + InputToken.StringValue + "\"]");
                    }
                    else
                    {
                        sb.AppendLine(GrammerLeftNode.Name + " ***wait input");
                    }
                }
            }
            else
            {
                tab_add = 1;

                sb.Append(tabchar, tabs);
                if (MatchedToken != null)
                {
                    sb.AppendLine(GrammerLeftNode.Name + tabchar + " [matched \"" + MatchedToken.StringValue + "\"]");
                }
                else
                {
                    if (InputToken != null)
                    {
                        sb.AppendLine(GrammerLeftNode.Name + " ***notmatched" + tabchar + " [input \"" + InputToken.StringValue + "\"]");
                    }
                    else
                    {
                        if (InputToken != null)
                        {
                            sb.AppendLine(GrammerLeftNode.Name + " ***notmatched" + tabchar + " [input \"" + InputToken.StringValue + "\"]");
                        }
                        else
                        {
                            sb.AppendLine(GrammerLeftNode.Name + " ***wait input");
                        }
                    }
                }
            }

            if (Nodes.Count > 0)
            { 
                foreach (var node in Nodes)
                {
                    sb.Append(node.GetTreeString(tabs+tab_add,tabchar));
                }
            }
            return sb.ToString();       
        }


        public static string getNodeValue(ParseExpr node)
        {
            if (node.GrammerLeftNode.Type == ParseNodeType.non_terminal)
            {
                var result = "";
                for (int i = 0; i < node.Nodes.Count; i++)
                {
                    result = result + getNodeValue(node.Nodes[i]);
                }
                return result;
            }
            else
            {
                return node.MatchedToken.StringValue;
            }
        }


    }
}
