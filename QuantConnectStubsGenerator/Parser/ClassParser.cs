using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using QuantConnectStubsGenerator.Model;
using QuantConnectStubsGenerator.Utility;

namespace QuantConnectStubsGenerator.Parser
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ClassParser : BaseParser
    {
        public ClassParser(ParseContext context, SemanticModel model) : base(context, model)
        {
        }

        protected override void EnterClass(BaseTypeDeclarationSyntax node)
        {
            var type = _typeConverter.GetType(node);

            // Prevent multiple registrations of partial classes
            if (_currentNamespace.HasClass(type))
            {
                _currentClass = _currentNamespace.GetClassByType(type);
                return;
            }

            var cls = ParseClass(node);

            if (_currentClass != null)
            {
                cls.ParentClass = _currentClass;
                _currentClass.InnerClasses.Add(cls);
            }

            _currentNamespace.RegisterClass(cls);
            _currentClass = cls;
        }

        private Class ParseClass(BaseTypeDeclarationSyntax node)
        {
            return new Class(_typeConverter.GetType(node))
            {
                Static = HasModifier(node, "static"),
                Summary = ParseSummary(node),
                Interface = node is InterfaceDeclarationSyntax,
                InheritsFrom = ParseInheritedTypes(node).ToList(),
                MetaClass = ParseMetaClass(node)
            };
        }

        private string ParseSummary(BaseTypeDeclarationSyntax node)
        {
            string summary = null;

            var doc = ParseDocumentation(node);
            if (doc["summary"] != null)
            {
                summary = doc["summary"].GetText();
            }

            if (HasModifier(node, "protected"))
            {
                summary = PrefixSummary(summary, "This class is protected.");
            }

            return summary;
        }

        private IEnumerable<PythonType> ParseInheritedTypes(BaseTypeDeclarationSyntax node)
        {
            if (node is EnumDeclarationSyntax)
            {
                yield return new PythonType("Enum", "enum");
            }

            var symbol = _model.GetDeclaredSymbol(node);

            if (symbol == null)
            {
                yield break;
            }

            if (symbol.BaseType != null)
            {
                var ns = symbol.BaseType.ContainingNamespace.Name;
                var name = symbol.BaseType.Name;

                // Don't make classes extend from System.Object, System.Enum or System.ValueType to reduce noise
                var isObject = ns == "System" && name == "Object";
                var isEnum = ns == "System" && name == "Enum";
                var isValueType = ns == "System" && name == "ValueType";

                if (!isObject && !isEnum && !isValueType)
                {
                    yield return _typeConverter.GetType(symbol.BaseType);
                }
            }

            foreach (var typeSymbol in symbol.Interfaces)
            {
                yield return _typeConverter.GetType(typeSymbol);
            }
        }

        private PythonType ParseMetaClass(BaseTypeDeclarationSyntax node)
        {
            if (node is InterfaceDeclarationSyntax || HasModifier(node, "abstract"))
            {
                return new PythonType("ABCMeta", "abc");
            }

            return null;
        }
    }
}