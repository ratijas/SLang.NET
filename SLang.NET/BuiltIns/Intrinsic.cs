using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;
using SLang.NET.Gen;
using Variable = SLang.NET.Gen.Variable;

namespace SLang.NET.BuiltIns
{
    public class Intrinsics : SLangUnitDefinition
    {
        public static readonly Identifier UnitName = new Identifier("$Intrinsics");

        public Intrinsics(Context ctx) : base(ctx, UnitName)
        {
        }

        public Callee GetCallee(Identifier methodName) =>
            new Callee(Name, methodName);

        public NativeRoutineDefinition IntrinsicAdd;
        public NativeRoutineDefinition IntrinsicSub;
        public NativeRoutineDefinition IntrinsicNeg;

        private SignatureReference unaryIntegerFuncSignature =>
            new SignatureReference(Context.TypeSystem.Integer,
                new[] {Context.TypeSystem.Integer});

        private SignatureReference binaryIntegerFuncSignature =>
            new SignatureReference(Context.TypeSystem.Integer,
                new[] {Context.TypeSystem.Integer, Context.TypeSystem.Integer});

        public override void Stage1RoutineStubs()
        {
            // add
            {
                var name = new Identifier("add");
                var method = new MethodDefinition(name.Value,
                    MethodAttributes.Public | MethodAttributes.Static,
                    Context.TypeSystem.Integer.NativeType);
                method.Parameters.Add(new ParameterDefinition(Context.TypeSystem.Integer.NativeType));
                method.Parameters.Add(new ParameterDefinition(Context.TypeSystem.Integer.NativeType));

                RegisterRoutine(IntrinsicAdd =
                    new NativeRoutineDefinition(this, name, binaryIntegerFuncSignature, method));
            }

            // sub
            {
                var name = new Identifier("sub");
                var method = new MethodDefinition(name.Value,
                    MethodAttributes.Public | MethodAttributes.Static,
                    Context.TypeSystem.Integer.NativeType);
                method.Parameters.Add(new ParameterDefinition(Context.TypeSystem.Integer.NativeType));
                method.Parameters.Add(new ParameterDefinition(Context.TypeSystem.Integer.NativeType));

                RegisterRoutine(IntrinsicSub =
                    new NativeRoutineDefinition(this, name, binaryIntegerFuncSignature, method));
            }

            // neg
            {
                var name = new Identifier("neg");
                var method = new MethodDefinition(name.Value,
                    MethodAttributes.Public | MethodAttributes.Static,
                    Context.TypeSystem.Integer.NativeType);
                method.Parameters.Add(new ParameterDefinition(Context.TypeSystem.Integer.NativeType));

                RegisterRoutine(IntrinsicNeg =
                    new NativeRoutineDefinition(this, name, unaryIntegerFuncSignature, method));
            }

            base.Stage1RoutineStubs();
        }

        public override void Stage2RoutineBody()
        {
            var integer = Context.TypeSystem.Integer;

            // add
            {
                var method = IntrinsicAdd;

                var ip = method.NativeMethod.Body.GetILProcessor();
                var result = new Variable(integer);
                method.NativeMethod.Body.InitLocals = true;

                // var result; // value type used by ref
                result.LoadA(ip);
                // lhs.value
                ip.Emit(OpCodes.Ldarg_0);
                ip.Emit(OpCodes.Ldfld, integer.ValueField);
                // rhs.value
                ip.Emit(OpCodes.Ldarg_1);
                ip.Emit(OpCodes.Ldfld, integer.ValueField);
                // lhs.value + rhs.value
                ip.Emit(OpCodes.Add);
                // result = new SLang$Integer(...)
                ip.Emit(OpCodes.Call, integer.Ctor);
                // return result
                result.Load(ip);
                ip.Emit(OpCodes.Ret);
            }

            // sub
            {
                var method = IntrinsicSub;

                var ip = method.NativeMethod.Body.GetILProcessor();
                var result = new Variable(integer);
                method.NativeMethod.Body.InitLocals = true;

                // var result; // value type used by ref
                result.LoadA(ip);
                // lhs.value
                ip.Emit(OpCodes.Ldarg_0);
                ip.Emit(OpCodes.Ldfld, integer.ValueField);
                // rhs.value
                ip.Emit(OpCodes.Ldarg_1);
                ip.Emit(OpCodes.Ldfld, integer.ValueField);
                // lhs.value - rhs.value
                ip.Emit(OpCodes.Sub);
                // result = new SLang$Integer(...)
                ip.Emit(OpCodes.Call, integer.Ctor);
                // return result
                result.Load(ip);
                ip.Emit(OpCodes.Ret);
            }

            // neg
            {
                var method = IntrinsicNeg;

                var ip = method.NativeMethod.Body.GetILProcessor();
                var v = new Variable(integer);
                method.NativeMethod.Body.InitLocals = true;

                // var result; // value type used by ref
                v.LoadA(ip);
                // self.value
                ip.Emit(OpCodes.Ldarg_0);
                ip.Emit(OpCodes.Ldfld, integer.ValueField);
                // -self.value
                ip.Emit(OpCodes.Neg);
                // result = new SLang$Integer(...)
                ip.Emit(OpCodes.Call, integer.Ctor);
                // return result
                v.Load(ip);
                ip.Emit(OpCodes.Ret);
            }

            base.Stage2RoutineBody();
        }
    }
}