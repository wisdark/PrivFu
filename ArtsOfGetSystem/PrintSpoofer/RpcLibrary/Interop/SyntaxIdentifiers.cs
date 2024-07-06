﻿using System;

namespace RpcLibrary.Interop
{
    internal class SyntaxIdentifiers
    {
        public static readonly RPC_SYNTAX_IDENTIFIER RpcUuidSyntax_1_0 = new RPC_SYNTAX_IDENTIFIER
        {
            SyntaxGUID = new Guid("12345678-1234-ABCD-EF00-0123456789AB"),
            SyntaxVersion = new RPC_VERSION { MajorVersion = 1, MinorVersion = 0 }
        };
        public static readonly RPC_SYNTAX_IDENTIFIER RpcTransferSyntax_2_0 = new RPC_SYNTAX_IDENTIFIER
        {
            SyntaxGUID = new Guid("8A885D04-1CEB-11C9-9FE8-08002B104860"),
            SyntaxVersion = new RPC_VERSION { MajorVersion = 2, MinorVersion = 0 }
        };
        public static readonly RPC_SYNTAX_IDENTIFIER RpcTransferSyntax64_2_0 = new RPC_SYNTAX_IDENTIFIER
        {
            SyntaxGUID = new Guid("71710533-BEBA-4937-8319-B5DBEF9CCC36"),
            SyntaxVersion = new RPC_VERSION { MajorVersion = 1, MinorVersion = 0 }
        };
    }
}
