using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.ReplaySorter.Sorting;

namespace ReplayParser.ReplaySorter.UserInput
{
    public class Answer
    {
        public Answer(bool? yesno = null, SortCriteriaParameters sortcriteriaparameters = null, Criteria chosencriteria = 0, bool? stopprogram = null)
        {
            Yes = yesno;
            SortCriteriaParameters = sortcriteriaparameters;
            ChosenCriteria = chosencriteria;
            StopProgram = stopprogram;
        }

        public Answer(SortCriteriaParameters sortcriteriaparameters)
        {
            SortCriteriaParameters = sortcriteriaparameters;
        }

        public Answer(Criteria chosencriteria, string[] criteriastringorder , bool stopprogram)
        {
            ChosenCriteria = chosencriteria;
            CriteriaStringOrder = criteriastringorder;
            StopProgram = stopprogram;
        }
        public Answer(bool? yesno)
        {
            Yes = yesno;
        }
        public bool? Yes { get; set; }

        public Criteria ChosenCriteria { get; set; }

        public string[] CriteriaStringOrder { get; set; }

        public SortCriteriaParameters SortCriteriaParameters { get; set; }

        public bool? StopProgram { get; set; }

    }
}
