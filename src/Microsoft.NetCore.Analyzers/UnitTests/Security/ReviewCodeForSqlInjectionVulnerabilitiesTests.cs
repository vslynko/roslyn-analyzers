﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.NetCore.Analyzers.Security.UnitTests
{
    using System;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.NetCore.Analyzers.Security;
    using Test.Utilities;
    using Xunit;

    public class ReviewCodeForSqlInjectionVulnerabilitiesTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ReviewCodeForSqlInjectionVulnerabilities();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new ReviewCodeForSqlInjectionVulnerabilities();
        }

        protected DiagnosticResult GetCSharpResultAt(int sinkLine, int sinkColumn, int sourceLine, int sourceColumn, string sink, string sinkContainingMethod, string source, string sourceContainingMethod)
        {
            this.PrintActualDiagnosticsOnFailure = true;
            return GetCSharpResultAt(
                new[] {
                    Tuple.Create(sinkLine, sinkColumn),
                    Tuple.Create(sourceLine, sourceColumn)
                },
                ReviewCodeForSqlInjectionVulnerabilities.Rule,
                sink,
                sinkContainingMethod,
                source,
                sourceContainingMethod);
        }

        protected void VerifyCSharpWithDependencies(string source, params DiagnosticResult[] expected)
        {
            this.VerifyCSharp(source, ReferenceFlags.AddTestReferenceAssembly, expected);
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_LocalString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input = Request.Form[""in""];
            if (Request.Form != null && !String.IsNullOrWhiteSpace(input))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = input,
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ",
                GetCSharpResultAt(20, 21, 15, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_DelegateInvocation_OutParam_LocalString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public delegate void StringOutputDelegate(string input, out string output);

        public static StringOutputDelegate StringOutput;

        protected void Page_Load(object sender, EventArgs e)
        {
            StringOutput(Request.Form[""in""], out string input);
            if (Request.Form != null && !String.IsNullOrWhiteSpace(input))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = input,
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ",
                GetCSharpResultAt(24, 21, 19, 26, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }


        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_InterfaceInvocation_OutParam_LocalString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public interface IBlah { void StringOutput(string input, out string output); }

        public static IBlah Blah;

        protected void Page_Load(object sender, EventArgs e)
        {
            Blah.StringOutput(Request.Form[""in""], out string input);
            if (Request.Form != null && !String.IsNullOrWhiteSpace(input))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = input,
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ",
                GetCSharpResultAt(24, 21, 19, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_LocalStringMoreBlocks_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input;
            if (Request.Form != null)
            {
                input = Request.Form[""in""];
            }
            else
            {
                input = ""SELECT 1"";
            }

            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(27, 17, 18, 25, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_And_QueryString_LocalStringMoreBlocks_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input;
            if (Request.Form != null)
            {
                input = Request.Form[""in""];
            }
            else
            {
                input = Request.QueryString[""in""];
            }

            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(27, 17, 18, 25, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"),
                GetCSharpResultAt(27, 17, 22, 25, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.QueryString", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Direct_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request.Form[""in""],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Substring_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request.Form[""in""].Substring(1),
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }


        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void Sanitized_HttpRequest_Form_Direct_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE id < "" + int.Parse(Request.Form[""in""]).ToString(),
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void Sanitized_HttpRequest_Form_TryParse_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Int16.TryParse(Request.Form[""in""], out short i))
            {
                SqlCommand sqlCommand = new SqlCommand()
                {
                    CommandText = ""SELECT * FROM users WHERE id < "" + i.ToString(),
                    CommandType = CommandType.Text,
                };
            }
        }
     }
}
            ");
        }


        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Item_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request[""in""],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Item_Enters_SqlParameters_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = @username"",
                CommandType = CommandType.Text,
            };

            sqlCommand.Parameters.Add(""@username"", SqlDbType.NVarChar, 16).Value = Request[""in""];

            sqlCommand.ExecuteReader();
        }
     }
}
            ");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Item_Sql_Constructor_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand(Request[""in""]);
        }
     }
}
            ",
                GetCSharpResultAt(15, 37, 15, 52, "SqlCommand.SqlCommand(string cmdText)", "void WebForm.Page_Load(object sender, EventArgs e)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }


        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Method_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string input = Request.Form.Get(""in"");
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(18, 17, 15, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_LocalNameValueCollectionString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            System.Collections.Specialized.NameValueCollection nvc = Request.Form;
            string input = nvc[""in""];
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(19, 17, 15, 70, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_List_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            List<string> allTheInputs = new List<string>(new string[] { Request.Form[""in""] });
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = allTheInputs[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(20, 17, 17, 73, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact(Skip = "Would be nice to distinguish between tainted and non-tainted elements in the List, but for now we taint the entire List from its construction.  FxCop also has a false positive.")]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_List_SafeElement_Diagnostic()
        {
            // Would be nice to distinguish between tainted and non-tainted elements in the List, but for now we taint the entire List from its construction.  FxCop also has a false positive.

            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            List<string> list = new List<string>(new string[] { Request.Form[""in""] });
            list.Add(""SELECT * FROM users WHERE userid = 1"");
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = list[1],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Array_List_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] array = new string[] { Request.Form[""in""] };
            List<string> allTheInputs = new List<string>(array);
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = allTheInputs[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(21, 17, 17, 45, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }


        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_Array_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] allTheInputs = new string[] { Request.Form[""in""] };
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = allTheInputs[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(20, 17, 17, 52, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_LocalStructNameValueCollectionString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public struct MyStruct
        {
            public NameValueCollection nvc;
            public string s;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            MyStruct myStruct = new MyStruct();
            myStruct.nvc = this.Request.Form;
            myStruct.s = myStruct.nvc[""in""];
            string input = myStruct.s;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(28, 17, 23, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_Form_LocalStructConstructorNameValueCollectionString_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"

namespace VulnerableWebApp
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public struct MyStruct
        {
            public MyStruct(NameValueCollection v)
            {
                this.nvc = v;
                this.s = null;
            }

            public NameValueCollection nvc;
            public string s;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            MyStruct myStruct = new MyStruct();
            myStruct.nvc = this.Request.Form;
            myStruct.s = myStruct.nvc[""in""];
            string input = myStruct.s;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(35, 17, 30, 28, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "NameValueCollection HttpRequest.Form", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_UserLanguages_Direct_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = Request.UserLanguages[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(17, 17, 17, 31, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string[] HttpRequest.UserLanguages", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_UserLanguages_LocalStringArray_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] languages = Request.UserLanguages;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = languages[0],
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(18, 17, 15, 34, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string[] HttpRequest.UserLanguages", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void HttpRequest_UserLanguages_LocalStringModified_Diagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string language = ""SELECT * FROM languages WHERE language = '"" + Request.UserLanguages[0] + ""'"";
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = language,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ",
                GetCSharpResultAt(18, 17, 15, 78, "string SqlCommand.CommandText", "void WebForm.Page_Load(object sender, EventArgs e)", "string[] HttpRequest.UserLanguages", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void OkayInputLocalStructNameValueCollectionString_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        public struct MyStruct
        {
            public NameValueCollection nvc;
            public string s;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            MyStruct myStruct = new MyStruct();
            myStruct.nvc = this.Request.Form;
            myStruct.s = myStruct.nvc[""in""];
            string input = myStruct.s;
            myStruct.s = ""SELECT 1"";
            input = myStruct.s;
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = input,
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void OkayInputConst_NoDiagnostic()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = 'foo'"",
                CommandType = CommandType.Text,
            };
        }
     }
}
            ");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void DataBoundLiteralControl_DirectImplementation_Text()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web.UI;

    public class SomeClass
    {
        public DataBoundLiteralControl Control { get; set; }

        public void Execute()
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = '"" + this.Control.Text + ""'"",
                CommandType = CommandType.Text,
            };
        }
    }
}
            ",
                GetCSharpResultAt(17, 17, 17, 74, "string SqlCommand.CommandText", "void SomeClass.Execute()", "string DataBoundLiteralControl.Text", "void SomeClass.Execute()"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void DataBoundLiteralControl_Interface_Text()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Web.UI;

    public class SomeClass
    {
        public DataBoundLiteralControl Control { get; set; }

        public void Execute()
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = '"" + ((ITextControl) this.Control).Text + ""'"",
                CommandType = CommandType.Text,
            };
        }
    }
}
            ",
                GetCSharpResultAt(17, 17, 17, 74, "string SqlCommand.CommandText", "void SomeClass.Execute()", "string ITextControl.Text", "void SomeClass.Execute()"));
        }


        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void SimpleInterprocedural()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            MyDatabaseLayer layer = new MyDatabaseLayer();
            layer.MakeSqlInjection(taintedInput);
        }
    }

    public class MyDatabaseLayer
    {
        public void MakeSqlInjection(string sqlInjection)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"",
                CommandType = CommandType.Text,
            };
        }
    }
}",
                GetCSharpResultAt(27, 17, 15, 35, "string SqlCommand.CommandText", "void MyDatabaseLayer.MakeSqlInjection(string sqlInjection)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void SimpleLocalFunction()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            SqlCommand injectSql(string sqlInjection)
            {
                return new SqlCommand()
                {
                    CommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"",
                    CommandType = CommandType.Text,
                };
            };

            injectSql(taintedInput);
        }
    }
}",
                GetCSharpResultAt(21, 21, 15, 35, "string SqlCommand.CommandText", "SqlCommand injectSql(string sqlInjection)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodReturnsTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            string sqlCommandText = StillTainted(taintedInput);

            ExecuteSql(sqlCommandText);
        }

        protected string StillTainted(string sqlInjection)
        {
            return ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodReturnsTaintedButOutputUntainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string sqlCommandText = StillTainted(taintedInput, out string notTaintedSqlCommandText);

            ExecuteSql(notTaintedSqlCommandText);
        }

        protected string StillTainted(string sqlInjection, out string notSqlInjection)
        {
            notSqlInjection = ""SELECT * FROM users WHERE userid = "" + Int32.Parse(sqlInjection);
            return ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodReturnsTaintedButRefUntainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string notTaintedSqlCommandText = taintedInput;
            string sqlCommandText = StillTainted(taintedInput, ref notTaintedSqlCommandText);

            ExecuteSql(notTaintedSqlCommandText);
        }

        protected string StillTainted(string sqlInjection, ref string notSqlInjection)
        {
            notSqlInjection = ""SELECT * FROM users WHERE userid = "" + Int32.Parse(sqlInjection);
            return ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodReturnsUntaintedButOutputTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string sqlCommandText = StillTainted(taintedInput, out string taintedSqlCommandText);

            ExecuteSql(taintedSqlCommandText);
        }

        protected string StillTainted(string input, out string sqlInjection)
        {
            sqlInjection = ""SELECT * FROM users WHERE username = '"" + input + ""'"";
            return ""SELECT * FROM users WHERE userid = "" + Int32.Parse(input);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
                GetCSharpResultAt(32, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodReturnsUntaintedButRefTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];
            
            string taintedSqlCommandText = null;
            string sqlCommandText = StillTainted(taintedInput, ref taintedSqlCommandText);

            ExecuteSql(taintedSqlCommandText);
        }

        protected string StillTainted(string input, ref string taintedSqlCommandText)
        {
            taintedSqlCommandText = ""SELECT * FROM users WHERE username = '"" + input + ""'"";
            return ""SELECT * FROM users WHERE userid = "" + Int32.Parse(input);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
                GetCSharpResultAt(33, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodReturnsNotTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            string sqlCommandText = NotTainted(taintedInput);

            ExecuteSql(sqlCommandText);
        }

        protected string NotTainted(string sqlInjection)
        {
            return ""SELECT * FROM users WHERE username = 'bob'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodSanitizesTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""userid""];

            string sqlCommandText = SanitizeTainted(taintedInput);

            ExecuteSql(sqlCommandText);
        }

        protected string SanitizeTainted(string sqlInjection)
        {
            return ""SELECT * FROM users WHERE userid = '"" + Int32.Parse(sqlInjection) + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodOutParameterTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 15, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodOutParameterNotTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            NotTainted(taintedInput, out string sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void NotTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = 'bob'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void IntermediateMethodOutParameterSanitizesTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""userid""];

            SanitizeTainted(taintedInput, out string sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void SanitizeTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE userid = '"" + Int32.Parse(sqlInjection) + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinaryReturnsDefaultStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            sqlCommandText = OtherDllStaticMethods.ReturnsDefault(sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(35, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinaryReturnsInputStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            sqlCommandText = OtherDllStaticMethods.ReturnsInput(sqlCommandText);

            ExecuteSql(sqlCommandText);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(34, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinarySetsOutputToDefaultStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            OtherDllStaticMethods.SetsOutputToDefault(sqlCommandText, out string sqlToExecute);

            ExecuteSql(sqlToExecute);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(35, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinarySetsReferenceToDefaultStillTainted()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            StillTainted(taintedInput, out string sqlCommandText);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlToExecute = null;
            OtherDllStaticMethods.SetsReferenceToDefault(sqlCommandText, ref sqlToExecute);

            ExecuteSql(sqlToExecute);
        }

        protected void StillTainted(string sqlInjection, out string sqlCommandText)
        {
            sqlCommandText = ""SELECT * FROM users WHERE username = '"" + sqlInjection + ""'"";
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(36, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Property_ConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ConstructedInput + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(29, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Property_Default()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.Default + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Method_ReturnsConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsConstructedInput() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(29, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Method_SetsOutputToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            otherDllObj.SetsOutputToConstructedInput(out string outputParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Method_SetsReferenceToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            string referenceParameter = ""not tainted"";
            otherDllObj.SetsReferenceToConstructedInput(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(32, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Method_ReturnsDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Method_SetsReferenceToDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string referenceParameter = ""not tainted"";
            otherDllObj.SetsReferenceToDefault(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(33, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_TaintedObject_Method_ReturnsDefault_UntaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(taintedInput);

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault(""not tainted"") + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Property_ConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ConstructedInput + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Property_Default()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.Default + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_ReturnsConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsConstructedInput() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            otherDllObj.SetsOutputToConstructedInput(out string outputParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_SetsReferenceToConstructedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string referenceParameter = ""also not tainted"";
            otherDllObj.SetsReferenceToConstructedInput(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_ReturnsDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault() + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_SetsReferenceToDefault()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainteed"");

            string referenceParameter = ""also not tainted"";
            otherDllObj.SetsReferenceToDefault(ref referenceParameter);

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + referenceParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_ReturnsDefault_UntaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault(""also not tainted"") + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}");
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_ReturnsDefault_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsDefault(taintedInput) + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_ReturnsInput_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsInput(taintedInput) + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(29, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_ReturnsRandom_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + otherDllObj.ReturnsRandom(taintedInput) + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToDefault_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            otherDllObj.SetsOutputToDefault(taintedInput, out string outputParameter);
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToInput_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            otherDllObj.SetsOutputToInput(taintedInput, out string outputParameter);
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(30, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }

        [Fact]
        [Trait(Traits.DataflowAnalysis, Traits.Dataflow.TaintedDataAnalysis)]
        public void CrossBinary_UntaintedObject_Method_SetsOutputToRandom_TaintedInput()
        {
            VerifyCSharpWithDependencies(@"
namespace VulnerableWebApp
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using OtherDll;

    public partial class WebForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string taintedInput = this.Request[""input""];

            OtherDllClass<string> otherDllObj = new OtherDllClass<string>(""not tainted"");

            // Still tainted, cuz not doing cross-binary interprocedural DFA.
            otherDllObj.SetsOutputToRandom(taintedInput, out string outputParameter);
            string sqlCommandText = ""SELECT * FROM users WHERE username = '"" + outputParameter + ""'"";

            ExecuteSql(sqlCommandText);
        }

        protected void ExecuteSql(string sqlCommandText)
        {
            SqlCommand sqlCommand = new SqlCommand()
            {
                CommandText = sqlCommandText,
                CommandType = CommandType.Text,
            };
        }
    }
}",
            GetCSharpResultAt(31, 17, 16, 35, "string SqlCommand.CommandText", "void WebForm.ExecuteSql(string sqlCommandText)", "string HttpRequest.this[string key]", "void WebForm.Page_Load(object sender, EventArgs e)"));
        }
    }
}