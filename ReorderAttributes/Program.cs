using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReorderAttributes
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: <thisscript> [-x] <file>");
                Console.Error.WriteLine("Sorts attributes in <file> and prints result to stdout");
                Console.Error.WriteLine("-x: Overwrite <file> instead of printing to stdout");
                return;
            }
            var overwrite = false;
            var pathOrOption = args[0];
            string path = pathOrOption;
            if (pathOrOption == "-x")
            {
                overwrite = true;
                path = args[1];
            }
            var result = ProcessFile(path);
            if (overwrite)
            {
                File.WriteAllText(path, result);
            }
            else
            {
                Console.WriteLine(result);
            }
        }

        static string ProcessFile(string path)
        {
            var before = File.ReadAllText(path);
            var after = OrderAttributeStructure(before);
            return after;
        }

        static string OrderAttributeStructure(string input)
        {
            var tree = CSharpSyntaxTree.ParseText(input);
            var root = tree.GetRoot();
            // First order each attribute list (e.g. "[A, C, B]" -> "[A, B, C]")
            root = new AttributeReorder().Visit(root);
            // Then order each attribute list, ordered by the first attribute of each list
            root = new AttributeListReorder().Visit(root);
            return root.ToString();
        }

        static string AttributeOrdering(AttributeSyntax attr)
        {
            return attr.GetText().ToString().Trim();
        }

        class AttributeReorder : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
            {
                var ordered = node.Attributes.OrderBy(x => x.Name.GetText().ToString());
                var result = node.WithAttributes(SyntaxFactory.SeparatedList(ordered));
                return base.VisitAttributeList(result);
            }
        }

        /// <summary>
        /// Seems no common access to a node's attributes, so separately writing accessors for each known pertinent node type
        /// </summary>
        class AttributeListReorder : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var result = ProcessAttributes(node.AttributeLists, node.WithAttributeLists);
                return base.VisitClassDeclaration(result);
            }

            public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
            {
                var result = ProcessAttributes(node.AttributeLists, node.WithAttributeLists);
                return base.VisitStructDeclaration(result);
            }

            public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                var result = ProcessAttributes(node.AttributeLists, node.WithAttributeLists);
                return base.VisitEnumDeclaration(result);
            }

            public override SyntaxNode VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
            {
                var result = ProcessAttributes(node.AttributeLists, node.WithAttributeLists);
                return base.VisitEnumMemberDeclaration(result);
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                var result = ProcessAttributes(node.AttributeLists, node.WithAttributeLists);
                return base.VisitMethodDeclaration(result);
            }

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                var result = ProcessAttributes(node.AttributeLists, node.WithAttributeLists);
                return base.VisitPropertyDeclaration(result);
            }

            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                var result = ProcessAttributes(node.AttributeLists, node.WithAttributeLists);
                return base.VisitFieldDeclaration(result);
            }

            public static T ProcessAttributes<T>(SyntaxList<AttributeListSyntax> attributes, Func<SyntaxList<AttributeListSyntax>, T> withAttributeLists)
            {
                var newAttributeList = attributes.OrderBy(l => AttributeOrdering(l.Attributes.First())).AsEnumerable();
                // Hack the whitespace together so it displays properly; we reordered the attributes but want to make sure the whitespace between still makes sense
                int i = 0;
                newAttributeList = newAttributeList.Select(x => x.WithTriviaFrom(attributes.ElementAt(i++)));
                var lists = SyntaxFactory.List<AttributeListSyntax>(newAttributeList);
                return withAttributeLists(lists);
            }
        }
    }
}
