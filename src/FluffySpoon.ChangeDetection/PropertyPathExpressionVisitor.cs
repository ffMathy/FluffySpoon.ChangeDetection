using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace FluffySpoon.ChangeDetection
{
    class PropertyPathExpressionVisitor : ExpressionVisitor
    {
        public ICollection<MemberInfo> Path { get; }

        public PropertyPathExpressionVisitor()
        {
            Path = new HashSet<MemberInfo>();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!(node.Member is PropertyInfo))
                throw new ArgumentException("The path can only contain properties.", nameof(node));

            Path.Add(node.Member);
            return base.VisitMember(node);
        }
    }
}
