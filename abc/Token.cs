namespace juicescript
{
    public class Token
    {
        public enum TokenType
        {
            /// <summary>
            /// 标识符
            /// </summary>
            identifier,

            /// <summary>
            /// 检查出是一个label
            /// </summary>
            label,

            /// <summary>
            /// 检查出是一个无用label
            /// </summary>
			useless_label,


			/// <summary>
			/// 检查出是一个this
			/// </summary>
			this_pointer,

            /// <summary>
            /// 检查出是一个super
            /// </summary>
            super_pointer,

            /// <summary>
            /// 字符串常量
            /// </summary>
            const_string,

            /// <summary>
            /// 内嵌正则表达式
            /// </summary>
            const_regexp,

            /// <summary>
            /// 内嵌XML
            /// </summary>
            const_xml,

            /// <summary>
            /// 数值常量
            /// </summary>
            const_number,

            /// <summary>
            /// 注释
            /// </summary>
            comments,

            /// <summary>
            /// 空白
            /// </summary>
            whitespace,

            /// <summary>
            /// 文件尾
            /// </summary>
            eof,

            /// <summary>
            /// 其他
            /// </summary>
            other
        }



        public Token preToken;
        public Token nextToken;


        public TokenType Type = TokenType.other;

        public int line;

        public int ptr;

        /// <summary>
        /// 字符串值
        /// </summary>
        public string StringValue = string.Empty;

        /// <summary>
        /// 源文件
        /// </summary>
        public string sourceFile;

        /// <summary>
        /// 源文件全路径
        /// </summary>
        public string sourceFileFullPath;


        public override string ToString()
        {
            return Type.ToString() + " " + StringValue;

        }

    }
}