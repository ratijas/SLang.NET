using System;
using Newtonsoft.Json.Linq;

namespace SLang.IR.JSON
{
    /// <summary>
    /// Parsing IR failed due to il-formatted code.
    /// </summary>
    public abstract class FormatException : Exception
    {
        protected FormatException(string message = null, Exception innerException = null) : base(message,
            innerException)
        {
        }
    }

    public class JsonFormatException : FormatException
    {
        public JsonEntity Entity { get; set; }

        public JsonFormatException(JsonEntity entity, string message = null, Exception innerException = null)
            : base(message, innerException)
        {
            Entity = entity;
        }
    }

    public class IrEntityException : FormatException
    {
        public Entity Entity { get; set; }

        public IrEntityException(Entity entity, string message = null, Exception innerException = null)
            : base(message, innerException)
        {
            Entity = entity;
        }
    }

    public class UnexpectedChildNodeException : IrEntityException
    {
        public Entity Child
        {
            get => Entity;
            set => Entity = value;
        }

        /// <summary>
        /// Parent is optional. For example, top-level entity does not have a parent.
        /// </summary>
        public Entity Parent { get; set; }

        public UnexpectedChildNodeException(
            Entity child,
            Entity parent = null,
            string message = null,
            Exception innerException = null)
            : base(child, message, innerException)
        {
            Parent = parent;
        }
    }
}