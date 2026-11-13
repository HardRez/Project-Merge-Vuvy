using System.Collections.Generic;

namespace MergeGame.Core.Rules
{
    public static class StandardDropRules
    {
        /// <summary>Move onto empty cells, merge equal items, reject everything else. Extra rules go in front of these.</summary>
        public static List<IDropRule> Create() => new List<IDropRule> { new MoveToEmptyCellRule(), new MergeRule() };
    }
}
