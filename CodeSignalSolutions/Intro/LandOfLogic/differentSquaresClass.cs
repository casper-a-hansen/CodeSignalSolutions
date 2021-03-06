/*
    Status:   Solved
    Imported: 2020-05-02 12:06
    By:       Casper
    Url:      https://app.codesignal.com/arcade/intro/level-12/fQpfgxiY6aGiGHLtv

    Description:
        Given a rectangular matrix containing only digits, calculate the number of
        different 2 × 2 squares in it.
        Example
        For
        matrix = [[1, 2, 1],
        [2, 2, 2],
        [2, 2, 2],
        [1, 2, 3],
        [2, 2, 1]]
        the output should be
        differentSquares(matrix) = 6.
        Here are all 6 different 2 × 2 squares:
        1 2
        2 2
        2 1
        2 2
        2 2
        2 2
        2 2
        1 2
        2 2
        2 3
        2 3
        2 1
        Input/Output
        [execution time limit] 3 seconds (cs)
        [input] array.array.integer matrix
        Guaranteed constraints:
        1 ≤ matrix.length ≤ 100,
        1 ≤ matrix[i].length ≤ 100,
        0 ≤ matrix[i][j] ≤ 9.
        [output] integer
        The number of different 2 × 2 squares in matrix.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeSignalSolutions.Intro.LandOfLogic
{
    class differentSquaresClass
    {
        int differentSquares(int[][] matrix) {
            HashSet<string> unique = new HashSet<string>();
            for(var y = 0; y < matrix.Length - 1; y++)
            {
                var row = matrix[y];
                for(var x = 0; x < row.Length - 1; x++)
                {
                    var box = row[x].ToString() + "," + row[x+1].ToString() + "," + matrix[y+1][x].ToString() + "," + matrix[y+1][x+1].ToString();
                    unique.Add(box);
                }
            }
            return unique.Count;
        }
    }
}
