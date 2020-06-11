using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KModkit;
using NUnit;
using UnityEngine;

public class TheMissingLetterScript : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio Audio;
    public KMRuleSeedable KMRuleSeedable;

    public KMSelectable[] Buttons;
    public TextMesh[] Labels;

    static int moduleIdCounter = 1;
    int moduleId;
    bool moduleSolved = false;
    int inpCount = 0;
    int result;

    List<char> alph = new List<char>();

    List<List<string>> Words = new List<List<string>>()
    {
        new List<string> { "Ecology", "Elderly", "Envious", "Episode", "Evening"},
        new List<string> { "Caramel", "Chariot", "Classic", "Collect", "Cryptic"},
        new List<string> { "Default", "Disrupt", "Donator", "Dungeon", "Dynasty"},
        new List<string> { "Fallout", "Flaunty", "Formula", "Fragile", "Funeral"},
        new List<string> { "Illicit", "Implant", "Insulin", "Iridium", "Isthmus"},
        new List<string> { "Garbage", "Giddyup", "Glutton", "Goodbye", "Grenade"},
        new List<string> { "Handout", "Heroism", "However", "Hubcaps", "Hypoxia"},
        new List<string> { "Jackpot", "Jealous", "Jinkies", "Journal", "Justice"},
        new List<string> { "Odyssey", "Oppress", "Oregano", "Outlook", "Overtly"},
        new List<string> { "Katydid", "Kenning", "Kimchee", "Kneepad", "Kumquat"},
        new List<string> { "Layaway", "Lettuce", "Liquefy", "Loyalty", "Luggage"},
        new List<string> { "Machine", "Mercury", "Minimum", "Momento", "Mystify"},
        new List<string> { "Narwhal", "Neutral", "Nirvana", "Nonzero", "Nucleus"},
        new List<string> { "Padlock", "Perfume", "Pilgrim", "Platypi", "Poverty"},
        new List<string> { "Ugliest", "Ukulele", "Umpteen", "Urinate", "Utility"},
        new List<string> { "Quarrel", "Quickly", "Quixote", "Quoting", "Qwertys"},
        new List<string> { "Radiate", "Realize", "Rickety", "Routine", "Rubbish"},
        new List<string> { "Savanna", "Scapula", "Seismic", "Shotgun", "Skeptic"},
        new List<string> { "Tabloid", "Templar", "Titrate", "Torpedo", "Tweezer"},
        new List<string> { "Vamoose", "Veranda", "Villain", "Volcano", "Vulpine"},
        new List<string> { "Abysmal", "Afghani", "Alchemy", "Alfalfa", "Amnesia"},
        new List<string> { "Warlock", "Welfare", "Whisper", "Worship", "Wriggle"},
        new List<string> { "Xanthan", "Xanthic", "Xeroxed", "Xiphoid", "Xylidin"},
        new List<string> { "Yapping", "Yeggman", "Yielded", "Yttrium", "Yummies"},
        new List<string> { "Zapping", "Zestier", "Zillion", "Zonking", "Zooming"},
        new List<string> { "Bangkok", "Beguile", "Biaxial", "Bluejay", "Bravado"}
    };

    List<Expression> rules = new List<Expression>();

    abstract class Expression
    {
        public abstract int Evaluate();
        public abstract override string ToString();
        public virtual bool NeedsParentheses { get { return true; } }
    }

    class Constant : Expression
    {
        public readonly int Value;
        public Constant(int value) { Value = value; }

        public override int Evaluate() { return Value; }
        public override string ToString() { return Value.ToString(); }
        public override bool NeedsParentheses { get { return false; } }

        public static readonly int[] _values = Enumerable.Range(1, 20).Where(i => i % 5 != 0).ToArray();
        public static Expression GetRandom(MonoRandom rnd)
        {
            return new Constant(_values[rnd.Next(0, _values.Length)]);
        }
    }

    class Variable : Expression
    {
        public readonly string Name;
        public readonly int Value;
        public Variable(string name, int value) { Name = name; Value = value; }

        public override int Evaluate() { return Value; }
        public override string ToString() { return Name; }
        public override bool NeedsParentheses { get { return false; } }
    }

    class Operator : Expression
    {
        public Expression Operand1;
        public Expression Operand2;
        public string OperatorName;
        public Func<int, int, int> Eval;
        public Operator(Expression op1, Expression op2, string opName, Func<int, int, int> eval)
        {
            Operand1 = op1;
            Operand2 = op2;
            OperatorName = opName;
            Eval = eval;
        }

        public override int Evaluate()
        {
            return Eval(Operand1.Evaluate(), Operand2.Evaluate());
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2} {6} {3}{4}{5}",
                Operand1.NeedsParentheses ? "(" : "", Operand1, Operand1.NeedsParentheses ? ")" : "",
                Operand2.NeedsParentheses ? "(" : "", Operand2, Operand2.NeedsParentheses ? ")" : "",
                OperatorName);
        }
    }

    class Add : Operator { public Add(Expression op1, Expression op2) : base(op1, op2, "+", (a, b) => a + b) { } }
    class Subtract : Operator { public Subtract(Expression op1, Expression op2) : base(op1, op2, "−", (a, b) => a - b) { } }
    class Multiply : Operator { public Multiply(Expression op1, Expression op2) : base(op1, op2, "×", (a, b) => a * b) { } }

    void Start()
    {
        moduleId = moduleIdCounter++;

        for (var i = 0; i < Buttons.Length; i++)
            Buttons[i].OnInteract += ButtonPressed(i);

        alph = Enumerable.Range(0, 26).Select(ch => (char) (ch + 'A')).ToList().Shuffle();

        for (var i = 0; i < Labels.Length; i++)
            Labels[i].text = alph[i].ToString();

        var sN = BombInfo.GetSerialNumber().Select(ch => ch < 'A' ? int.Parse(ch.ToString()) : ch - 'A' + 1).ToList();

        var variables = new List<Variable>();

        for (var ltr = 0; ltr < 25; ltr++)
            variables.Add(new Variable(string.Format("A{0}", ltr + 1), alph[ltr] - 'A' + 1));
        for (var repeat = 0; repeat < 3; repeat++)
        {
            variables.Add(new Variable("BH", BombInfo.GetBatteryHolderCount()));
            variables.Add(new Variable("B", BombInfo.GetBatteryCount()));
            variables.Add(new Variable("PP", BombInfo.GetPortPlateCount()));
            variables.Add(new Variable("P", BombInfo.GetPortCount()));
            variables.Add(new Variable("I", BombInfo.GetIndicators().Count()));
            variables.Add(new Variable("UI", BombInfo.GetOffIndicators().Count()));
            variables.Add(new Variable("LI", BombInfo.GetOnIndicators().Count()));
            for (var sn = 0; sn < sN.Count; sn++)
                variables.Add(new Variable(string.Format("S{0}", sn + 1), sN[sn]));
        }

        //Ruleseed start
        var rnd = KMRuleSeedable.GetRNG();
        for (var ltr = 0; ltr < 26; ltr++)
        {
            var operatorCountDecider = rnd.NextDouble();
            var operatorCount = operatorCountDecider < .2 ? 0 : operatorCountDecider < .7 ? 1 : 2;

            Expression expr = variables[rnd.Next(0, variables.Count)];
            for (var o = 0; o < operatorCount; o++)
            {
                var otherOperand = rnd.NextDouble() < .3 ? Constant.GetRandom(rnd) : variables[rnd.Next(0, variables.Count)];
                if (rnd.Next(0, 2) != 0)
                {
                    var t = expr;
                    expr = otherOperand;
                    otherOperand = t;
                }
                switch (rnd.Next(0, 3))
                {
                    case 0: expr = new Add(expr, otherOperand); break;
                    case 1: expr = new Subtract(expr, otherOperand); break;
                    case 2: expr = new Multiply(expr, otherOperand); break;
                }
            }

            rules.Add(expr);
        }
        //Ruleseed end

        result = Math.Abs(rules[alph.Last() - 'A'].Evaluate()) % 5 + 1;

        Debug.LogFormat(@"[The Missing Letter #{0}] Using rule seed: {1}", moduleId, rnd.Seed);
        Debug.LogFormat(@"[The Missing Letter #{0}] Equations are:{1}{2}", moduleId, Environment.NewLine,
            Enumerable.Range(0, 26).Select(rule => string.Format(@"[The Missing Letter #{0}] {1}: {2}", moduleId, (char) ('A' + rule), rules[rule])).Join(Environment.NewLine));
        Debug.LogFormat(@"[The Missing Letter #{0}] The missing letter is {1}", moduleId, alph.Last());
        Debug.LogFormat(@"[The Missing Letter #{0}] N = {1}", moduleId, result);
        Debug.LogFormat(@"[The Missing Letter #{0}] Word to enter is: ""{1}""", moduleId, Words[alph.Last() - 'A'][result - 1]);

    }

    private KMSelectable.OnInteractHandler ButtonPressed(int btn)
    {
        return delegate ()
        {
            if (moduleSolved)
                return false;

            if (char.ToUpperInvariant(Words[alph.Last() - 'A'][result - 1][inpCount]).Equals(alph[btn]))
                inpCount++;
            else
            {

                Debug.LogFormat(@"[The Missing Letter #{0}] Entered: ""{1}"" - Expected : ""{2}""", moduleId, alph[btn], Words[alph.Last() - 'A'][result - 1][inpCount]);
                BombModule.HandleStrike();
                inpCount = 0;
            }
            if (inpCount == Words[alph.Last() - 'A'][result - 1].Length)
            {
                BombModule.HandlePass();
                moduleSolved = true;
            }
            return false;
        };
    }
}
