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
        public NativeRoutineDefinition StandardIO_put_Integer;
        public NativeRoutineDefinition StandardIO_put_String;
        

        private SignatureReference unaryIntegerFuncSignature =>
            new SignatureReference(Context.TypeSystem.Integer,
                new[] {Context.TypeSystem.Integer});

        private SignatureReference binaryIntegerFuncSignature =>
            new SignatureReference(Context.TypeSystem.Integer,
                new[] {Context.TypeSystem.Integer, Context.TypeSystem.Integer});

        private SignatureReference integerConsumerSignature =>
            new SignatureReference(Context.TypeSystem.Void,
                new[] {Context.TypeSystem.Integer});

        private SignatureReference stringConsumerSignature =>
            new SignatureReference(Context.TypeSystem.Void,
                new[] {Context.TypeSystem.String});

        private TypeDefinition consoleNativeType =>
            Context.NativeModule.ImportReference(
                new TypeReference(
                    nameof(System),
                    nameof(Console),
                    null,
                    Context.NativeModule.TypeSystem.CoreLibrary)).Resolve();


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

            // StandardIO$put$Integer
            {
                var name = new Identifier("StandardIO$put$Integer");
                var method = new MethodDefinition(name.Value,
                    MethodAttributes.Public | MethodAttributes.Static,
                    Context.TypeSystem.Void.NativeType);
                method.Parameters.Add(new ParameterDefinition(Context.TypeSystem.Integer.NativeType));

                RegisterRoutine(StandardIO_put_Integer =
                    new NativeRoutineDefinition(this, name, integerConsumerSignature, method));
            }
            
            // StandardIO$put$String
            {
                var name = new Identifier("StandardIO$put$String");
                var method = new MethodDefinition(name.Value,
                    MethodAttributes.Public | MethodAttributes.Static,
                    Context.TypeSystem.Void.NativeType);
                method.Parameters.Add(new ParameterDefinition(Context.TypeSystem.String.NativeType));

                RegisterRoutine(StandardIO_put_String =
                    new NativeRoutineDefinition(this, name, stringConsumerSignature, method));
            }


            base.Stage1RoutineStubs();
        }

        public override void Stage2RoutineBody()
        {
            var integer = Context.TypeSystem.Integer;
            var str = Context.TypeSystem.String;

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

            // StandardIO$put$Integer
            {
                // direct proxy to void Console.Write(Int32)
                var method = StandardIO_put_Integer;

                var ip = method.NativeMethod.Body.GetILProcessor();
                var writeForeign = consoleNativeType.Methods.Single(
                    m => m.Name == nameof(Console.Write) &&
                         m.Parameters.Count == 1 &&
                         m.Parameters[0].ParameterType.Name ==
                         Context.TypeSystem.Integer.WrappedNativeType.Name);
                var writeImported = Context.NativeModule.ImportReference(writeForeign);

                // unbox argument
                ip.Emit(OpCodes.Ldarg_0);
                ip.Emit(OpCodes.Ldfld, integer.ValueField);
                // call method
                ip.Emit(OpCodes.Call, writeImported);
                // return nothing
                ip.Emit(OpCodes.Ret);
            }
            
            // StandardIO$put$String
            {
                // direct proxy to void Console.Write(String)
                var method = StandardIO_put_String;

                var ip = method.NativeMethod.Body.GetILProcessor();
                var writeForeign = consoleNativeType.Methods.Single(
                    m => m.Name == nameof(Console.Write) &&
                         m.Parameters.Count == 1 &&
                         m.Parameters[0].ParameterType.Name ==
                         Context.TypeSystem.String.WrappedNativeType.Name);
                var writeImported = Context.NativeModule.ImportReference(writeForeign);

                // unbox argument
                ip.Emit(OpCodes.Ldarg_0);
                ip.Emit(OpCodes.Ldfld, str.ValueField);
                // call method
                ip.Emit(OpCodes.Call, writeImported);
                // return nothing
                ip.Emit(OpCodes.Ret);
            }

            base.Stage2RoutineBody();
        }
    }
}