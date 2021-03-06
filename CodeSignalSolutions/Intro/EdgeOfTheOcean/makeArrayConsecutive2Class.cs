/*
    Status:   Solved
    Imported: 2020-05-02 11:53
    By:       Casper
    Url:      https://app.codesignal.com/arcade/intro/level-2/bq2XnSr5kbHqpHGJC

    Description:
        Ratiorg got statues of different sizes as a present from CodeMaster for his
        birthday, each statue having an non-negative integer size. Since he likes to
        make things perfect, he wants to arrange them from smallest to largest so that
        each statue will be bigger than the previous one exactly by 1. He may need some
        additional statues to be able to accomplish that. Help him figure out the
        minimum number of additional statues needed.
        Example
        For statues = [6, 2, 3, 8], the output should be
        makeArrayConsecutive2(statues) = 3.
        Ratiorg needs statues of sizes 4, 5 and 7.
        Input/Output
        [execution time limit] 3 seconds (cs)
        [input] array.integer statues
        An array of distinct non-negative integers.
        Guaranteed constraints:
        1 ≤ statues.length ≤ 10,
        0 ≤ statues[i] ≤ 20.
        [output] integer
        The minimal number of statues that need to be added to existing statues such
        that it contains every integer size from an interval [L, R] (for some L, R) and
        no other sizes.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeSignalSolutions.Intro.EdgeOfTheOcean
{
    class makeArrayConsecutive2Class
    {
        int makeArrayConsecutive2(int[] statues) {
            Array.Sort(statues);
            var result = 0;
            for(var i = 0; i < statues.Length - 1; i++)
            {
                result += statues[i + 1]  - statues[i] - 1;
            }
            return result;
        }
    }
}
