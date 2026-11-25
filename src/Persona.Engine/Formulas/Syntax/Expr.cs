namespace Persona.Engine.Formulas.Syntax;

internal enum BinaryOp
{
    ADD, // +
    SUB, // -
    MULT, // *
    DIV, // /
}

internal enum ComparisonOp
{
    GRTTHN, // >
    GRTEQ, // >=
    LSSTHN, // <
    LSSEQ, // <=
    EQL, // ==
    NOTEQ, // !=
}

internal enum FormulaFunction
{
    MIN,
    MAX,
    FLOOR
}

internal abstract record Expr
{
    // Leaves
    internal sealed record Number(decimal Value) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            return (Value, null);
        }

        public override FormulaType TypeOf()
        {
            return FormulaType.Number;
        }
    }
    internal sealed record Variable(string Name) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            if (variables.ContainsKey(Name))
            {
                return (variables[Name], null);
            }

            throw new KeyNotFoundException($"No variable named {Name} available.");
        }

        public override FormulaType TypeOf()
        {
            return FormulaType.Number;
        }
    }

    // Unary
    internal sealed record Negate(Expr Operand) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            var (oDec, oBool) = Operand.Eval(variables);

            if (oDec != null)
            {
                return (-(oDec.Value), null);
            }
            else if (oBool != null)
            {
                throw new InvalidEvaluationResultException("Cannot do boolean NOT operation");
            }

            throw new InvalidDataException("Cannot evaluate operand to negate from either bool or decimal.");
        }

        public override FormulaType TypeOf()
        {
            if (Operand.TypeOf() != FormulaType.Number)
                throw Fail($"Negate needs numbers, not true/false values.");
            return FormulaType.Number;
        }
    }

    // Binary, split by result type
    internal sealed record Arithmetic(BinaryOp Op, Expr Left, Expr Right) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            var (lDec, lBool) = Left.Eval(variables);
            var (rDec, rBool) = Right.Eval(variables);

            if (lDec != null && rDec != null)
            {
                var lValue = lDec.Value;
                var rValue = rDec.Value;

                switch (Op)
                {
                    case BinaryOp.ADD:
                        return (lValue + rValue, null);
                    case BinaryOp.SUB:
                        return (lValue - rValue, null);
                    case BinaryOp.MULT:
                        return (lValue * rValue, null);
                    case BinaryOp.DIV:
                        if (rValue == 0)
                        {
                            throw new DivideByZeroException("Cannot divide by zero");
                        }
                        return (Math.Floor(lValue / rValue), null);
                }
            }
            else
            {
                throw new InvalidEvaluationResultException("Cannot perform arithmetic with boolean values");
            }

            throw new InvalidOperationException("Op not set to a handled value");
        }

        public override FormulaType TypeOf()
        {
            if (Left.TypeOf() != FormulaType.Number || Right.TypeOf() != FormulaType.Number)
            {
                throw Fail("Arithmetic Expression needs numbers, not true/false values.");
            }
            return FormulaType.Number;
        }
    }

    internal sealed record Comparison(ComparisonOp Op, Expr Left, Expr Right) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            var (lDec, lBool) = Left.Eval(variables);
            var (rDec, rBool) = Right.Eval(variables);

            if (lDec != null && rDec != null)
            {
                var lValue = lDec.Value;
                var rValue = rDec.Value;

                switch (Op)
                {
                    case ComparisonOp.GRTTHN:
                        return (null, lValue > rValue);
                    case ComparisonOp.GRTEQ:
                        return (null , lValue >= rValue);
                    case ComparisonOp.LSSTHN:
                        return (null, lValue < rValue);
                    case ComparisonOp.LSSEQ:
                        return (null, lValue <= rValue);
                    case ComparisonOp.NOTEQ:
                        return (null, lValue != rValue);
                    case ComparisonOp.EQL:
                        return (null, lValue == rValue);
                }
            }
            else if (lBool != null && rBool != null)
            {
                var lValue = lBool.Value;
                var rValue = rBool.Value;

                switch (Op)
                {
                    case ComparisonOp.NOTEQ:
                        return (null, lValue != rValue);

                    // Can be Bool or Decimal
                    case ComparisonOp.EQL:
                        return (null, lValue == rValue);

                    default:
                        throw new InvalidEvaluationResultException($"Cannot compare Boolean values by {Op.ToString()}");
                }
            }
            else
            {
                throw new InvalidEvaluationResultException("Cannot compare Boolean and Decimal values.");
            }


            throw new InvalidOperationException("Op not set to a handled value");
        }

        public override FormulaType TypeOf()
        {
            return CheckComparison();
        }

        private FormulaType CheckComparison()
        {
            var l = Left.TypeOf();
            var r = Right.TypeOf();
            if (l != r)
                throw Fail("Cannot compare a number with a true/false value.");
            if (l == FormulaType.Boolean && Op is not (ComparisonOp.EQL or ComparisonOp.NOTEQ))
                throw Fail("True/false values can only be compared with == or !=.");
            return FormulaType.Boolean;
        }
    }

    // Functions one record each
    internal sealed record Min(Expr Left, Expr Right) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            var (lDec, lBool) = Left.Eval(variables);
            var (rDec, rBool) = Right.Eval(variables);

            if (lDec != null && rDec != null)
            {
                return (Math.Min(lDec.Value, rDec.Value), null);
            }
            else
            {
                throw new InvalidEvaluationResultException("Cannot evaluate Min() with boolean values");
            }
        }

        public override FormulaType TypeOf()
        {
            if (Left.TypeOf() != FormulaType.Number || Right.TypeOf() != FormulaType.Number)
            {
                throw Fail("Min Function needs numbers, not true/false values.");
            }
            return FormulaType.Number;
        }
    }
    internal sealed record Max(Expr Left, Expr Right) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            var (lDec, lBool) = Left.Eval(variables);
            var (rDec, rBool) = Right.Eval(variables);

            if (lDec != null && rDec != null)
            {
                return (Math.Max(lDec.Value, rDec.Value), null);
            }
            else
            {
                throw new InvalidEvaluationResultException("Cannot evaluate Max() with boolean values");
            }
        }

        public override FormulaType TypeOf()
        {
            if (Left.TypeOf() != FormulaType.Number || Right.TypeOf() != FormulaType.Number)
            {
                throw Fail("Max Function needs numbers, not true/false values.");
            }
            return FormulaType.Number;
        }
    }
    internal sealed record Floor(Expr Operand) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            var (oDec, oBool) = Operand.Eval(variables);

            if (oDec != null)
            {
                return (Math.Floor(oDec.Value), null);
            }
            else
            {
                // Throw?
                throw new InvalidEvaluationResultException("Cannot evaluate a bool expression with Floor()");
            }
        }

        public override FormulaType TypeOf()
        {
            if (Operand.TypeOf() != FormulaType.Number)
            {
                throw Fail("Floor Function needs numbers, not true/false values.");
            }
            return FormulaType.Number;
        }
    }
    internal sealed record Has(string VariableName) : Expr
    {
        internal override (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables)
        {
            return (null, variables.ContainsKey(VariableName));
        }

        public override FormulaType TypeOf()
        {
            return FormulaType.Boolean;
        }
    }

    internal abstract (decimal?, bool?) Eval(IReadOnlyDictionary<string, decimal> variables);
    public abstract FormulaType TypeOf();

    internal static FormulaSyntaxException Fail(string message) => new(message);
}

internal sealed class FormulaSyntaxException(string message) : Exception(message);
internal sealed class InvalidEvaluationResultException(string message) : Exception(message);
