using juicescript.compiler.parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace juicescript.compiler.parse
{
    /// <summary>
    /// 文法符号
    /// </summary>
    public class ParseNode : IEquatable<ParseNode>
    {
        private int hashcode;

        public readonly string Name;

        public readonly ParseNodeType Type;

        public ParseNode( string name, ParseNodeType type)
        {
            this.Name = name;
            this.Type = type;
            hashcode = type.GetHashCode() ^ name.GetHashCode();
        }

        public static ParseNode GNodeNull = new ParseNode("null", ParseNodeType._null); 
        public static ParseNode GNodeNumber = new ParseNode("number", ParseNodeType.number); 
        public static ParseNode GNodeString = new ParseNode("string", ParseNodeType.conststring); 
        public static ParseNode GNodeIdentifier = new ParseNode("identifier", ParseNodeType.identifier); 
        public static ParseNode GNodeWrong = new ParseNode("wrong", ParseNodeType.wrong); 
        public static ParseNode GNodeEOF = new ParseNode("$$", ParseNodeType.eof); 
        public static ParseNode GNodeWhiteSpace = new ParseNode("S", ParseNodeType.whitespace);
        public static ParseNode GNodeLabel = new ParseNode("label", ParseNodeType.label); 

        public static ParseNode GNodeUseLessLabel = new ParseNode("useless_label", ParseNodeType.useless_label);

		public static ParseNode GNodeThis = new ParseNode("this", ParseNodeType._this); 
        public static ParseNode GNodeSuper = new ParseNode("super", ParseNodeType.super);

        public ListSet<ParseNode> FIRST = new ListSet<ParseNode>();
        public ListSet<ParseNode> FOLLOW = new ListSet<ParseNode>();

        public override int GetHashCode()
        {
            return hashcode;
        }

        public override string ToString()
        {
            return string.Format("{0},\ttype:{1}", Name, Type);
        }

        public bool Equals(ParseNode other)
        {
            return Type == other.Type && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is ParseNode)
            {
                return Equals((ParseNode)obj);
            }
            else
            {
                return false;
            }
        }

    }
}
