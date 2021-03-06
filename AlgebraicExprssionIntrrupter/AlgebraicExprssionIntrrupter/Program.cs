﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Math;

namespace AlgebraicExprssionIntrrupter
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    var treeEx = AlgebExpression.Parse(Console.ReadLine());
                    var solution = treeEx.GetSolution();
                    switch (solution.type)
                    {
                        case SolutionType.Some:
                            foreach (var sol in solution.solutions)
                                Console.WriteLine(sol);
                            break;
                        case SolutionType.All:
                            Console.WriteLine("any x");
                            break;
                        case SolutionType.None:
                            Console.WriteLine("none");
                            break;
                    }

                }
                catch (FormatException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                }
               //catch (Exception ex)
               //{
               //    Console.WriteLine(ex.Message);
               //}
                Console.WriteLine("=========================================");
            }
        }
    }

    enum Operation
    {
        Equality,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Value
    }

    #region disable pragmas
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    #endregion

    /// <summary>
    /// of the shape (float)x^(int)
    /// </summary>
    struct AlgebVal
    {
        public float Coaf;
        public int Pow;
        /// <summary>
        /// reperesents the 0 for the monoid that is AlgebVal
        /// </summary>
        public static AlgebVal Zero = new AlgebVal(0, 0);
        public static AlgebVal One = new AlgebVal(1, 0);

        public AlgebVal(float coaf, int pow)
        {
            Coaf = coaf;
            Pow = coaf == 0 ? 0 : pow;
        }

        /// <summary>
        /// of the shape (float)x^(int)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static AlgebVal Parse(string s)
        {
            if (!s.Contains('x'))
                s = s + "x^0";
            if (s.EndsWith("x"))
                s = s + "^1";
            if (s.StartsWith("x"))
                s = "1" + s;
            var sa = s.Split('x', '^');
            float f; int i;
            var b = Single.TryParse(sa[0], out f);
            b &= Int32.TryParse(sa[2], out i);
            if (b)
                return new AlgebVal(f, i);
            else
                throw new FormatException("must be of shape (float)x^(int)");
        }

        public static bool TryParse(string s, out AlgebVal av)
        {
            Regex avreg = new Regex(@"^\-?[0-9\.]*x?\^?[0-9]*$");
            if (avreg.IsMatch(s))
            {
                if (!s.Contains('x'))
                    s = s + "x^0";
                if (s.EndsWith("x"))
                    s = s + "^1";
                if (s.StartsWith("x"))
                    s = "1" + s;
                av = Parse(s);
                return true;
            }
            av = new AlgebVal(0, 0);
            return false;
        }

        #region Operators
        public static AlgebVal operator +(AlgebVal left, AlgebVal right)
        {
            if (left.Coaf == 0)
                return right;
            if (right.Coaf == 0)
                return left;
            if (left.Pow != right.Pow)
                throw new InvalidOperationException("must have same power");
            return new AlgebVal(left.Coaf + right.Coaf, left.Pow);
        }

        public static AlgebVal operator *(AlgebVal left, AlgebVal right)
        {
            return new AlgebVal(left.Coaf * right.Coaf, left.Pow + right.Pow);
        }

        public static AlgebVal operator /(AlgebVal left, AlgebVal right)
        {
            if (left.Pow >= right.Pow)
                return new AlgebVal(left.Coaf / right.Coaf, left.Pow - right.Pow);
            else throw new ArgumentException("can't m8");
        }

        public static AlgebVal operator -(AlgebVal left, AlgebVal right)
        {
            if (left.Pow != right.Pow && left.Coaf != 0 && right.Coaf != 0)
                throw new InvalidOperationException("must have same power");
            return new AlgebVal(left.Coaf - right.Coaf, left.Pow == 0 ? right.Pow : left.Pow);
        }

        public static bool operator ==(AlgebVal left, AlgebVal right) =>
            (left.Coaf == 0 && right.Coaf == 0)
                ? true
                : left.Coaf == right.Coaf && left.Pow == right.Pow;

        public static bool operator !=(AlgebVal left, AlgebVal right) =>
            !(left == right);

        public static implicit operator AlgebVal(float i) =>
            new AlgebVal(i, 0);

        public override string ToString() =>
            this == Zero ? "0" : (Coaf == 0 ? "" : Coaf.ToString()) + ((Pow > 1) ? "x^" + Pow.ToString() : Pow == 1 ? "x" : "");
        #endregion
    }

    /// <summary>
    /// represents an algeb expression
    /// </summary>
    class AlgebExpression
    {
        public AlgebExpression left;
        public Operation op;
        public AlgebExpression right;
        public AlgebVal? value;

        private List<float> OutOfDomain { get; }
        
        public Solution GetSolution()
        {
            return this
                .SimplifyAll()
                .ToTrinom()
                .FindSolution();                
        }

        #region Simplifing & helpers

        public AlgebExpression SimplifyAll() =>
            this
            .Factor()
            .SimplifyEquality()
            .SubtructionToNegAddition()
            .SimplifyDivision()
            .SimplifyMultiplication()
            .LeftAlignAddition()
            .SimplifyZeroAddition()
            .SimplifyAddition();

        public Trinom ToTrinom()
        {
            if (this.op == Operation.Equality)
                return Trinom.Parse(this.left.ToString());
            return Trinom.Parse(this.ToString());
        }

        public AlgebExpression SimplifyAddition()
        {
            var tree = this.LeftAlignAddition();
            if (tree.op == Operation.Equality)
                return new AlgebExpression
                    (this.left.SimplifyAddition(),
                    Operation.Equality,
                    right);
            if (tree.op == Operation.Value)
                return this;
            tree.left = tree.left.SimplifyAddition();
            tree = tree.left.LookAndAddValue(right.value.Value);
            return tree;
        }
        public AlgebExpression SimplifyZeroAddition()
        {
            if (this.op == Operation.Value)
                return this;
            var temp = this.LeftAlignAddition();
            if (this.op != Operation.Equality)
            {
                if (temp.right.value.Value.Coaf == 0)
                    return temp.left.SimplifyZeroAddition();
                else
                    return temp.left.SimplifyZeroAddition() + temp.right;
            }
            else
            {
                if (temp.right.value.Value.Coaf == 0)
                    return
                        new AlgebExpression(
                        temp.left.SimplifyZeroAddition(),
                        Operation.Equality,
                        0);
                else
                    return
                        new AlgebExpression(
                        temp.left.SimplifyZeroAddition() + temp.right,
                        Operation.Equality,
                        0);
            }
        }
        public AlgebExpression LeftAlignAddition()
        {
            if (this.op == Operation.Equality)
            {
                var leftleft = left.LeftAlignAddition();
                return new AlgebExpression(this.left.LeftAlignAddition(), Operation.Equality, right.LeftAlignAddition());
            }
            if (this.op == Operation.Value)
                return this;
            if (this.op != Operation.Addition)
                throw new InvalidOperationException("to align addition all ops must be addition");
            if (this.right.op == Operation.Value)
                return left.LeftAlignAddition() + right;
            if (this.left.op == Operation.Value)
                return right.LeftAlignAddition() + left;
            if (left.op == Operation.Addition && right.op == Operation.Addition)
            {
                left = left.LeftAlignAddition();
                right = right.LeftAlignAddition();
                if (this.right.op == Operation.Value)
                    return left.LeftAlignAddition() + right;
                if (this.left.op == Operation.Value)
                    return right.LeftAlignAddition() + left;

                return new AlgebExpression
                    (((left.left + right.right)
                        + left.right)
                        + right.left)
                        .LeftAlignAddition();
            }
            else
            {
                throw new InvalidOperationException("to align addition all ops must be addition");
            }
            throw new Exception("sumthing went wrong");
        }
        private AlgebExpression LookAndAddValue(AlgebVal val)
        {
            if (val.Coaf == 0)
                return this;
            if (this.op == Operation.Value)
                return this + val;
            if (this.op == Operation.Addition)
            {
                if (this.right.value.Value.Coaf == 0)
                    return left.LookAndAddValue(val);
                else if (this.right.value.Value.Pow == val.Pow)
                    return left + (right.value.Value + val);
                else
                    return left.LookAndAddValue(val) + right;
            }
            else
                throw new InvalidOperationException("can only be done on addition trees");
        }        
        public AlgebExpression SubtructionToNegAddition()
        {
            if (this.op == Operation.Value)
                return value;
            this.left = left.SubtructionToNegAddition();
            this.right = right.SubtructionToNegAddition();
            if (this.op == Operation.Subtraction)
                return new AlgebExpression(left, Operation.Addition, right * -1);
            return this;
        }

        public AlgebExpression SimplifyMultiplication()
        {
            if (op == Operation.Value)
                return this;
            left = left.SimplifyMultiplication();
            right = right.SimplifyMultiplication();
            if (op == Operation.Multiplication)
                return left * right;
            return this;
        }

        public AlgebExpression Factor()
        {
            if (this.op == Operation.Value)
                return this;
            if (this.op == Operation.Equality)
                return new AlgebExpression(
                    this.left.Factor(),
                    Operation.Equality,
                    this.right.Factor());
            try
            {
                var trinom = this.SubtructionToNegAddition().SimplifyAddition().ToTrinom();
                if (trinom.a != 0)
                {
                    var fst = trinom.FindSolution().solutions[0] * -1;
                    var snd = trinom.FindSolution().solutions[1] * -1;
                    string s1 = fst.ToString();
                    string s2 = snd.ToString();
                    if (fst >= 0)
                        s1 = "+" + fst.ToString();
                    if (snd >= 0)
                        s2 = "+" + snd.ToString();
                    string toParse = $"({trinom.a}*(x{s1})*(x{s2}))";
                    if (trinom.a == 1)
                        toParse = $"((x{s1})*(x{s2}))";
                    var rtn = AlgebExpression.Parse(toParse);
                    return rtn;
                }
                else if (trinom.b != 0)
                {
                    var fst = trinom.FindSolution().solutions[0] * -1;
                    string s1 = fst.ToString();
                    if (fst >= 0)
                        s1 = "+" + fst.ToString();
                    string toParse;
                    if (trinom.b == 1)
                        toParse = $"(x{s1})";
                    else
                        toParse = $"({trinom.b}*(x{s1}))";
                    var rtn = AlgebExpression.Parse(toParse);
                    return rtn;
                }
                return this;
            }
            catch
            {
                return new AlgebExpression(
                    this.left.Factor(),
                    op,
                    right.Factor()
                    );
            }
        }
        public AlgebExpression SimplifyDivision()
        {
            if (this.ContainsOp(Operation.Division))
            {
                var myThis = this;
                return SimplifyDivsion(this, ref myThis, true).SimplifyDivision();
            }
            return this;
        }
        private static AlgebExpression SimplifyDivsion(AlgebExpression currTree, ref AlgebExpression bigTree, bool rtnBig)
        {
            if (currTree.op == Operation.Value)
                return currTree;

            currTree.left = SimplifyDivsion(currTree.left, ref bigTree, false);
            currTree.right = SimplifyDivsion(currTree.right, ref bigTree, false);

            if (currTree.op == Operation.Division)
            {
                if (currTree.left.IsDivisibleBy(currTree.right))
                    return SimplifyDivsion((currTree.left / currTree.right), ref bigTree, false);
                if (currTree.IsPartOf(bigTree))
                    bigTree = (bigTree * currTree.right).Factor();
            }
            return rtnBig ? bigTree : currTree;
        }
        
        public bool IsDivisibleBy(AlgebExpression tree)
        {
            if (tree == null || this == null)
                return false;
            if (this.op == Operation.Value && tree.op == Operation.Value && this.value.Value.Pow >= tree.value.Value.Pow)
                return this.value.Value.Pow >= tree.value.Value.Pow;
            if (this.op == Operation.Value)
                return false;
            if (this == tree)
                return true;
            else
            {
                if (this.op == Operation.Multiplication)
                    return
                        this.left.IsDivisibleBy(tree)
                        || this.right.IsDivisibleBy(tree);
                if (this.op == Operation.Addition || this.op == Operation.Subtraction)
                {
                    var newThis = this.SubtructionToNegAddition();
                    tree = tree.SubtructionToNegAddition();
                    return (newThis.left.IsDivisibleBy(tree)
                            && newThis.right.IsDivisibleBy(tree));
                }
            }
            return false;
        }
        private bool IsPartOf(AlgebExpression bigTree)
        {
            if (this == bigTree)
                return true;
            if (bigTree.op == Operation.Value)
                return false;
            return
                this.IsPartOf(bigTree.left)
                || this.IsPartOf(bigTree.right);
        }
        private bool ContainsOp(Operation operation)
        {
            if (this.op == operation)
                return true;
            if (this.op == Operation.Value)
                return false;
            return this.left.ContainsOp(operation) || this.right.ContainsOp(operation);
        }

        public AlgebExpression SimplifyEquality()
        {
            if (op == Operation.Equality)
                if (right != AlgebVal.Zero)
                    return this - right;
            return this;
        }


        #endregion

        #region ctors
        public AlgebExpression(AlgebExpression l, Operation o, AlgebExpression r)
        {
            left = l; op = o; right = r;
        }

        public AlgebExpression(AlgebVal val)
        {
            op = Operation.Value;
            value = val;
        }

        public AlgebExpression(AlgebExpression tree)
        {
            left = tree.left; op = tree.op; right = tree.right; value = tree.value;
        }
        #endregion

        public static AlgebExpression Parse(string s)
        {
            //checking if it is of the right format
            s = new string(s.ToLower().Where(c => c != ' ').ToArray());
            //TODO: write better regex, if possible. else, find a better thing to check for correctness.
            Regex alexreg = new Regex(@"^[\(\)0-9\.\-\+/\*x^]+=?[\(\)0-9\.\-\+/\*x^]*$");
            if (!alexreg.IsMatch(s))
                throw new FormatException("bad format");
            //init values
            AlgebVal v = AlgebVal.Zero;
            int bracketCount = 0;

            if (s.StartsWith("-"))
                s = "0" + s;

            if (s.Count(c => c == '(') != s.Count(c => c == ')'))
                throw new FormatException("parens must balance");
            if (s.StartsWith("(") && s.EndsWith(")"))
            {
                //Checking if I get expression inside paren that balance eachother.
                int amountOfParen = 0;
                bool above0 = true;
                foreach (var c in s.Substring(1, s.Length - 2))
                {
                    if (c == '(')
                        amountOfParen++;
                    if (c == ')')
                        amountOfParen--;
                    if (amountOfParen < 0)
                    {
                        above0 = false;
                        break;
                    }
                }
                if (above0)
                    return Parse(s.Substring(1, s.Length - 2));
            }

            if (AlgebVal.TryParse(s, out v))
                return new AlgebExpression(v);


            #region =
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == '(')
                    bracketCount++;
                if (s[i] == ')')
                    bracketCount--;
                if (bracketCount == 0)
                {
                    if (s[i] == '=')
                        return new AlgebExpression(
                            Parse(s.Substring(0, i)),
                            Operation.Equality,
                            Parse(s.Substring(i + 1, s.Length - (i + 1))));
                }
            }
            #endregion
            #region +-
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == '(')
                    bracketCount++;
                if (s[i] == ')')
                    bracketCount--;
                if (bracketCount == 0)
                {
                    if (s[i] == '+')
                        return new AlgebExpression(
                            Parse(s.Substring(0, i))
                            +
                            Parse(s.Substring(i + 1, s.Length - (i + 1))));

                    if (s[i] == '-')
                        return new AlgebExpression(
                            Parse(s.Substring(0, i))
                            -
                            Parse(s.Substring(i + 1, s.Length - (i + 1))));
                }
            }
            #endregion
            #region */
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == '(')
                    bracketCount++;
                if (s[i] == ')')
                    bracketCount--;
                if (bracketCount == 0)
                {
                    if (s[i] == '*')
                        return new AlgebExpression(
                            Parse(s.Substring(0, i)),
                            Operation.Multiplication,
                            Parse(s.Substring(i + 1, s.Length - (i + 1))));

                    if (s[i] == '/')
                        return new AlgebExpression(
                            Parse(s.Substring(0, i))
                            /
                            Parse(s.Substring(i + 1, s.Length - (i + 1))));
                }
            }
            #endregion

            throw new Exception();
        }

        #region Operators

        public static implicit operator AlgebExpression(AlgebVal av) =>
            new AlgebExpression(av);

        public static implicit operator AlgebExpression(float i) =>
            new AlgebExpression(i);

        public static bool operator ==(AlgebExpression left, AlgebExpression right)
        {
            if ((object)left == null || (object)right == null)
                return false;

            if (left.op == Operation.Value && right.op == Operation.Value)
                return left.value == right.value;


            if (left.op == Operation.Addition && left.left == 0)
                return left.right == right;
            if ((left.op == Operation.Addition || left.op == Operation.Subtraction) && left.right == 0)
                return left.left == right;

            if (right.op == Operation.Addition && right.left == 0)
                return right.right == left;
            if ((right.op == Operation.Addition || right.op == Operation.Subtraction) && right.right == 0)
                return right.left == left;

            if (left.op == Operation.Multiplication && left.left == 1)
                return left.right == right;
            if (left.op == Operation.Multiplication || left.op == Operation.Division && left.right == 1)
                return left.left == right;

            if (right.op == Operation.Multiplication && right.left == 1)
                return right.right == left;
            if (right.op == Operation.Multiplication || right.op == Operation.Division && right.right == 1)
                return right.left == left;


            else
                return
                    left.op == right.op
                    &&
                    ((left.left == right.left
                    && right.right == left.right)
                    ||
                    ((left.left == right.right
                    && right.left == left.right)
                        && (
                        left.op == Operation.Addition
                        || right.op == Operation.Multiplication
                        )
                    )
                    );
        }

        public static bool operator !=(AlgebExpression left, AlgebExpression right) =>
            !(left == right);

        public static AlgebExpression operator +(AlgebExpression left, AlgebExpression right)
        {
            if (left.op == Operation.Value
                && right.op == Operation.Value
                &&
                    (left.value.Value.Pow == right.value.Value.Pow
                    || left.value.Value.Coaf == 0
                    || right.value.Value.Coaf == 0))
                return new AlgebExpression(left.value.Value + right.value.Value);

            if (left.op == Operation.Equality && right.op == Operation.Equality)
                throw new InvalidOperationException("can't be both =");

            if (left.op == Operation.Equality)
                return new AlgebExpression(left.left + right, Operation.Equality, left.right + right);
            if (right.op == Operation.Equality)
                return new AlgebExpression(right.left + left, Operation.Equality, right.right + left);

            return new AlgebExpression(left, Operation.Addition, right);
        }

        public static AlgebExpression operator -(AlgebExpression left, AlgebExpression right)
        {
            if (left.op == Operation.Value
                && right.op == Operation.Value
                &&
                    (left.value.Value.Pow == right.value.Value.Pow
                    || left.value.Value.Coaf == 0
                    || right.value.Value.Coaf == 0))
                return new AlgebExpression(left.value.Value - right.value.Value);

            if (left.op == Operation.Equality && right.op == Operation.Equality)
                throw new InvalidOperationException("can't be both =");

            if (left.op == Operation.Equality)
                return new AlgebExpression(left.left - right, Operation.Equality, left.right - right);
            if (right.op == Operation.Equality)
                return new AlgebExpression(right.left - left, Operation.Equality, right.right - left);

            if (right == left)
                return new AlgebExpression(AlgebVal.Zero);
            if (left.op == Operation.Value
                && left.value.Value.Coaf == 0)
                return right * -1;

            return new AlgebExpression(left, Operation.Subtraction, right);
        }

        public static AlgebExpression operator -(AlgebExpression expr) =>
            -1 * expr;

        public static AlgebExpression operator *(AlgebExpression left, AlgebExpression right)
        {
            #region commented code 
            /*
            if (left.op == Operation.Value
                && right.op == Operation.Value)
                return new AlgebExpression(left.value.Value * right.value.Value);

            if (left.op == Operation.Value && left.value.Value == AlgebVal.One)
                return right;
            if (right.op == Operation.Value && right.value.Value == AlgebVal.One)
                return left;

            if (left.op == Operation.Equality && right.op == Operation.Equality)
                throw new InvalidOperationException("can't be both =");

            if (left.op == Operation.Equality)
                return new AlgebExpression(left.left * right, Operation.Equality, left.right * right);
            if (right.op == Operation.Equality)
                return new AlgebExpression(right.left * left, Operation.Equality, right.right * left);


            // huh? why? 
            //if (left.op == Operation.Value)
            //    return new AlgebExpression(left.value.Value * right.left);
            //
            //if (right.op == Operation.Value)
            //    return new AlgebExpression(right.value.Value * left.left);


            if (left.op == Operation.Division)
            {
                if (left.right.IsDivisibleBy(right))
                    return left.left / (left.right / right);
                if (right.IsDivisibleBy(left.right))
                    return (right / left.right) * left.left;
            }

            if (right.op == Operation.Division)
            {
                if (right.right.IsDivisibleBy(left))
                    return right.left / (right.right / left);
                if (right.IsDivisibleBy(left.right))
                    return (left / right.right) * right.left;
            }


            if (left.op == Operation.Addition || left.op == Operation.Subtraction)
                return new AlgebExpression(left.left * right, left.op, left.right * right);
            if (right.op == Operation.Addition || right.op == Operation.Subtraction)
                return new AlgebExpression(right.left * left, right.op, right.right * left);

            if (left.op == Operation.Multiplication)
                return new AlgebExpression(left.left * right, Operation.Multiplication, left.right);
            if (right.op == Operation.Multiplication)
                return new AlgebExpression(right.left * left, Operation.Multiplication, right.right);
            */
            #endregion

            if (left.op == Operation.Equality && right.op == Operation.Equality)
                throw new InvalidOperationException("Can not multiply two equalities");
            if (left.op == Operation.Equality)
                return new AlgebExpression(
                    left.left * right, Operation.Equality, left.right * right
                    );
            if (right.op == Operation.Equality)
                return new AlgebExpression(
                    right.left * left, Operation.Equality, right.right * left
                    );

            if ((left.op == Operation.Value && left.value.Value == AlgebVal.Zero)
                                            ||
                (right.op == Operation.Value && right.value.Value == AlgebVal.Zero))
                return AlgebVal.Zero;
            if (left.op == Operation.Value && left.value.Value == AlgebVal.One)
                return right;
            if (right.op == Operation.Value && right.value.Value == AlgebVal.One)
                return left;

            if (left.op == Operation.Value && right.op == Operation.Value)
                return new AlgebExpression(
                    left.value.Value * right.value.Value
                    );


            if (left.op == Operation.Division)
            {
                if (right.IsDivisibleBy(left.right) || right.CanCancelWith(left.right))
                    return (right / left.right) * left.left;
                else return (right * left.left) / left.right;
            }
            if (right.op == Operation.Division)
            {
                if (left.IsDivisibleBy(right.right) || left.CanCancelWith(right.right))
                    return (left / right.right) * right.left;
                else return (left * right.left) / right.right;
            }

            if (left.op == Operation.Addition || left.op == Operation.Subtraction)
                return new AlgebExpression(
                    left.left * right, left.op, left.right * right
                    );
            if (right.op == Operation.Addition || right.op == Operation.Subtraction)
                return new AlgebExpression(
                    right.left * left, right.op, right.right * left
                    );
            if (left.op == Operation.Multiplication)
                return new AlgebExpression(
                    left.left * right, Operation.Multiplication, left.right
                    );
            if (right.op == Operation.Multiplication)
                return new AlgebExpression(
                    right.left * left, Operation.Multiplication, right.right
                    );

            return new AlgebExpression(left, Operation.Multiplication, right);
        }

        public static AlgebExpression operator /(AlgebExpression left, AlgebExpression right)
        {
            #region commented code
            /*
            // v/1 => v
            if (right.op == Operation.Value && right.value.Value == AlgebVal.One)
                return left;

            // t/t => 1
            if (left == right)
                return new AlgebVal(1, 0);

            //value division
            if (left.op == Operation.Value
                && right.op == Operation.Value
                && left.value.Value.Pow >= right.value.Value.Pow)
                return new AlgebExpression(left.value.Value / right.value.Value);

            //canceling
            if (left.IsDivisibleBy(right))
            {
                if (left.op == Operation.Multiplication)
                {
                    if (left.left.IsDivisibleBy(right))
                        return left.right * (left.left / right);
                    if (left.right.IsDivisibleBy(right))
                        return left.left * (left.right / right);
                }
                //todo: go over this and add behavior for betterness
                if (left.op == Operation.Addition)
                {
                    if (left.left.IsDivisibleBy(right) &&
                        left.right.IsDivisibleBy(right))
                        return (left.left / right) + (left.right / right);
                    if (right.op == Operation.Addition)
                    {
                        if (left.left.IsDivisibleBy(right.left)
                            && left.right.IsDivisibleBy(right.right))
                            return (left.left / right.left) + (left.right / right.right);
                        if (left.left.IsDivisibleBy(right.right)
                            && left.right.IsDivisibleBy(right.left))
                            return (left.left / right.right) + (left.right / right.left);
                    }
                    if (right.op == Operation.Subtraction)
                    {
                        if (left.left.IsDivisibleBy(right.left)
                            && left.right.IsDivisibleBy(right.right))
                            return (left.left / right.left) - (left.right / right.right);
                        if (left.left.IsDivisibleBy(right.right)
                            && left.right.IsDivisibleBy(right.left))
                            return (left.left / right.right) - (left.right / right.left);
                    }
                }
                if (left.op == Operation.Subtraction)
                {
                    if (left.left.IsDivisibleBy(right) &&
                        left.right.IsDivisibleBy(right))
                        return (left.left / right) - (left.right / right);
                    if (right.op == Operation.Addition)
                    {
                        if (left.left.IsDivisibleBy(right.left)
                            && left.right.IsDivisibleBy(right.right))
                            return (left.left / right.left) - (left.right / right.right);
                        if (left.left.IsDivisibleBy(right.right)
                            && left.right.IsDivisibleBy(right.left))
                            return (left.left / right.right) - (left.right / right.left);
                    }
                    if (right.op == Operation.Subtraction)
                    {
                        if (left.left.IsDivisibleBy(right.left)
                            && left.right.IsDivisibleBy(right.right))
                            return (left.left / right.left) + (left.right / right.right);
                        if (left.left.IsDivisibleBy(right.right)
                            && left.right.IsDivisibleBy(right.left))
                            return (left.left / right.right) + (left.right / right.left);
                    }
                }
            }

            if (left.op == Operation.Equality && right.op == Operation.Equality)
                throw new InvalidOperationException("can't be both =");

            if (left.op == Operation.Equality)
                return new AlgebExpression(left.left / right, Operation.Equality, left.right / right);

            if (right.op == Operation.Equality)
                return new AlgebExpression(right.left / left, Operation.Equality, right.right / left);

            //   if (left.op == Operation.Addition || left.op == Operation.Subtraction)
            //       return new AlgebExpression(left.left / right, left.op, left.right / right);
            return new AlgebExpression(left, Operation.Division, right);
            */
            #endregion

            if (left.op == Operation.Equality && right.op == Operation.Equality)
                throw new InvalidOperationException("Can not divide two equations");
            if (left.op == Operation.Equality)
                return new AlgebExpression(
                    left.left / right, Operation.Equality, left.right / right
                    );
            if (right.op == Operation.Equality)
                return new AlgebExpression(
                    right.left / left, Operation.Equality, right.right / left
                    );

            if (left == right)
                return AlgebVal.One;
            if (left.op == Operation.Value && left.value.Value == AlgebVal.Zero)
                return AlgebVal.Zero;
            if (left.IsDivisibleBy(right))
            {
                if (left.op == Operation.Value && right.op == Operation.Value)
                    return left / right;
                if (left.op == Operation.Multiplication)
                {
                    if (left.left.IsDivisibleBy(right))
                        return (left.left / right) * left.right;
                    if (left.right.IsDivisibleBy(right))
                        return left.left * (left.right / right);
                }

                if (left.op == Operation.Addition)
                {
                    if (left.left.IsDivisibleBy(right) && left.right.IsDivisibleBy(right))
                        return (left.left / right) + (left.right / right);
                }
            }

            if (left.CanCancelWith(right))
            {
                if (left.op == Operation.Multiplication)
                {
                    if (left.left.CanCancelWith(right))
                        return (left.left / right) * left.right;
                    if (left.right.CanCancelWith(right))
                        return (left.right / right) * left.left;
                }
                if (right.op == Operation.Multiplication)
                {
                    if (right.left.CanCancelWith(left))
                        return (left / right.left) / right.right;
                    if (right.right.CanCancelWith(left))
                        return (left / right.right) / right.left;
                }
            }

            if (right.op == Operation.Division)
                return left * (right.right / right.left);

            return new AlgebExpression(left, Operation.Division, right);
        }

        /// <summary>
        /// only for multiplication canceling
        /// </summary>
        /// <param name="right"></param>
        /// <returns></returns>
        private bool CanCancelWith(AlgebExpression tree)
        {
            if (this.IsDivisibleBy(tree))
                return true;
            else if (this.op == Operation.Value || tree.op == Operation.Value)
                return false;
            if (this.op == Operation.Multiplication)
                return this.left.CanCancelWith(tree)
                    || this.right.CanCancelWith(tree);
            if (tree.op == Operation.Multiplication)
                return tree.CanCancelWith(this.right)
                    || tree.CanCancelWith(this.left);
            return false;
        }

        public override string ToString()
        {
            switch (op)
            {
                case Operation.Value:
                    return value.Value.ToString();
                case Operation.Equality:
                    return left.ToString() + "=" + right.ToString();
                case Operation.Addition:
                    return "(" + left.ToString() + "+" + right.ToString() + ")";
                case Operation.Subtraction:
                    return "(" + left.ToString() + "-" + right.ToString() + ")";
                case Operation.Multiplication:
                    return "(" + left.ToString() + "*" + right.ToString() + ")";
                case Operation.Division:
                    return "(" + left.ToString() + "/" + right.ToString() + ")";
            }
            return "";
        }

        #endregion
    }

    #region resotre pragmas
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    #endregion


    /// <summary>
    /// represents a trinom
    /// </summary>
    class Trinom
    {
        public float a, b, c;
        public Trinom(float _a, float _b, float _c)
        {
            a = _a; b = _b; c = _c;
        }

        /// <summary>
        /// finds the solution
        /// </summary>
        /// <returns></returns>
        public Solution FindSolution()
        {
            if (a == 0.0f)
                if (b == 0.0f)
                    if (c == 0.0f)
                        return new Solution(SolutionType.All);
                    else return new Solution(SolutionType.None);
                else return new Solution(new[] { -(c / b) });

            if (b * b - 4 * a * c >= 0)
                return new Solution(new[]
                {
                    (float)(-b + Sqrt(b * b - 4 * a * c)) / (a * 2.0f),
                    (float)(-b - Sqrt(b * b - 4 * a * c)) / (a * 2.0f)
                });
            else
                throw new InvalidOperationException("delta must be >= 0");
        }

        public static Trinom Parse(string s)
        {
            var vals = new string(s.Where(c => c != '(' && c != ')').ToArray()).Split('+');
            if (vals.Length > 3)
                throw new InvalidOperationException("must be TRInom");
            var tri = new Trinom(0, 0, 0);
            foreach (var val in vals)
            {
                var algval = AlgebVal.Parse(val);
                switch (algval.Pow)
                {
                    case 0:
                        tri.c = algval.Coaf;
                        break;
                    case 1:
                        tri.b = algval.Coaf;
                        break;
                    case 2:
                        tri.a = algval.Coaf;
                        break;
                    default:
                        throw new InvalidOperationException("must be up to 2nd power");
                }
            }
            return tri;
        }

    }

    class Solution
    {
        public SolutionType type;
        public float[] solutions;

        public Solution(SolutionType _type)
        {
            type = _type;
        }

        public Solution(float[] fs)
        {
            solutions = fs; type = SolutionType.Some;
        }
    }

    enum SolutionType
    {
        Some,
        All,
        None,
    }
}
