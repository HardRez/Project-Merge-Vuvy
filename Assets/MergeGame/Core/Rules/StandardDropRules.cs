using System.Collections.Generic;

namespace MergeGame.Core.Rules
{
    public static class StandardDropRules
    {
        /// <summary>
        /// Keep locked items in place, keep closed boxes out of reach, move onto empty cells, merge equal items, reject everything else.
        /// Extra rules go in front of these.
        /// </summary>
        public static List<IDropRule> Create() =>
            new List<IDropRule> { new LockedSourceRule(), new HiddenTargetRule(), new MoveToEmptyCellRule(), new MergeRule() };
    }
}
