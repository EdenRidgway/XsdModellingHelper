namespace Specs_for_TopologicalSorter
{
    using XsdHelper;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_provided_with_unordered_set_of_dependencies
    {
        // Depedencies: A > B, B > C, B > D, C > D
        // Sorted Items: D, C, B, A
        List<Edge<string>> _inputList = new List<Edge<string>>
                                                {
                                                    new Edge<string>("C", "D"),
                                                    new Edge<string>("B", "D"),
                                                    new Edge<string>("A", "B"),
                                                    new Edge<string>("B", "C"),
                                                };

        [Test]
        public void Should_return_the_results_from_no_depedencies_backwards()
        {
            TopologicalEdgeSorter<string> sorter = new TopologicalEdgeSorter<string>();
            IEnumerable<string> sortedList = sorter.Sort(_inputList);

            Assert.That(new string[] {"D", "C", "B", "A"}, Is.EqualTo(sortedList));
        }
    }
}