﻿using System.Collections.Generic;
using Google.OrTools.LinearSolver;

namespace Yafc.Model {
    public class SolverHelper<TVariable, TConstraint>(bool maximize) where TVariable : notnull where TConstraint : notnull {
        private readonly Dictionary<(TVariable var, TConstraint constr), float> values = [];
        private readonly List<(TVariable var, float min, float max, float coef)> variables = [];
        private readonly List<(TConstraint constr, float min, float max)> constraints = [];
        private readonly bool maximize = maximize;

        private readonly Dictionary<TVariable, float> results = [];

        public float this[TVariable var, TConstraint constr] {
            get => values.TryGetValue((var, constr), out float val) ? val : 0;
            set => values[(var, constr)] = value;
        }

        public float this[TVariable var] => results.TryGetValue(var, out float value) ? value : 0f;

        public void AddVariable(TVariable var, float min, float max, float coef) {
            variables.Add((var, min, max, coef));
        }

        public void AddConstraint(TConstraint constr, float min, float max) {
            constraints.Add((constr, min, max));
        }

        public void Clear() {
            values.Clear();
            variables.Clear();
            constraints.Clear();
        }

        public Solver.ResultStatus Solve() {
            var solver = DataUtils.CreateSolver();
            results.Clear();
            Dictionary<TVariable, Variable> realMapVars = new Dictionary<TVariable, Variable>(variables.Count);
            Dictionary<TConstraint, Constraint> realMapConstrs = new Dictionary<TConstraint, Constraint>(constraints.Count);
            var objective = solver.Objective();
            objective.SetOptimizationDirection(maximize);

            foreach (var (tvar, min, max, coef) in variables) {
                var variable = solver.MakeNumVar(min, max, tvar.ToString());
                objective.SetCoefficient(variable, coef);
                realMapVars[tvar] = variable;
            }

            foreach (var (tconst, min, max) in constraints) {
                var constraint = solver.MakeConstraint(min, max, tconst.ToString());
                realMapConstrs[tconst] = constraint;
            }

            foreach (var ((tvar, tconstr), value) in values) {
                if (realMapVars.TryGetValue(tvar, out var variable) && realMapConstrs.TryGetValue(tconstr, out var constraint)) {
                    constraint.SetCoefficient(variable, value);
                }
            }

            var result = solver.Solve();

            if (result is Solver.ResultStatus.OPTIMAL or Solver.ResultStatus.FEASIBLE) {
                foreach (var (tvar, var) in realMapVars) {
                    results[tvar] = (float)var.SolutionValue();
                }
            }
            solver.Dispose();

            return result;
        }
    }
}
