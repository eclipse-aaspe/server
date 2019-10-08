using AdminShellNS;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasOpcUaServer
{
    public class AasUaEntityFileType : AasUaBaseEntity
    {
        public AasUaEntityFileType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as they shall be all there in the UA constants
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.File file)
        {
            // access
            if (parent == null || file == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "File", ReferenceTypeIds.HasComponent, ObjectTypeIds.FileType);

            // populate attributes from the spec
            /*
            this.entityBuilder.CreateAddPropertyState<string>("MimeType", DataTypeIds.String, file.mimeType, ReferenceTypeIds.HasProperty, o.NodeId, VariableIds.FileType_MimeType);
            this.entityBuilder.CreateAddPropertyState<UInt16>("OpenCount", DataTypeIds.UInt16, 0, ReferenceTypeIds.HasProperty, o.NodeId, VariableIds.FileType_OpenCount);
            this.entityBuilder.CreateAddPropertyState<UInt64>("Size", DataTypeIds.UInt64, 0, ReferenceTypeIds.HasProperty, o.NodeId, VariableIds.FileType_Size);
            this.entityBuilder.CreateAddPropertyState<bool>("UserWritable", DataTypeIds.Boolean, true, ReferenceTypeIds.HasProperty, o.NodeId, VariableIds.FileType_UserWritable);
            this.entityBuilder.CreateAddPropertyState<bool>("Writable", DataTypeIds.Boolean, true, ReferenceTypeIds.HasProperty, o.NodeId, VariableIds.FileType_Writable);
            */
            this.entityBuilder.CreateAddPropertyState<string>(o, "MimeType", DataTypeIds.String, file.mimeType, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);
            this.entityBuilder.CreateAddPropertyState<UInt16>(o, "OpenCount", DataTypeIds.UInt16, 0, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);
            this.entityBuilder.CreateAddPropertyState<UInt64>(o, "Size", DataTypeIds.UInt64, 0, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType, valueRank: -1);
            this.entityBuilder.CreateAddPropertyState<bool>(o, "UserWritable", DataTypeIds.Boolean, true, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);
            this.entityBuilder.CreateAddPropertyState<bool>(o, "Writable", DataTypeIds.Boolean, true, ReferenceTypeIds.HasProperty, VariableTypeIds.PropertyType);

            // Open
            var mOpen = this.entityBuilder.CreateAddMethodState(o, "Open",
                inputArgs: new Argument[] {
                    new Argument("Mode", DataTypeIds.Byte, -1, "")
                },
                outputArgs: new Argument[] {
                    new Argument("FileHandle", DataTypeIds.UInt32, -1, "")
                }, referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            mOpen.Executable = true;
            mOpen.UserExecutable = true;
            mOpen.WriteMask = AttributeWriteMask.None;
            mOpen.UserWriteMask = AttributeWriteMask.None;
            mOpen.RolePermissions = new RolePermissionTypeCollection();
            mOpen.UserRolePermissions = new RolePermissionTypeCollection();
            mOpen.OnCallMethod = OnReadCalled;
            mOpen.OnReadExecutable = IsResumeExecutable;
            mOpen.OnReadUserExecutable = IsResumeUserExecutable;

            // Close
            var mClose = this.entityBuilder.CreateAddMethodState(o, "Close",
                inputArgs: new Argument[] {
                    new Argument("FileHandle", DataTypeIds.UInt32, -1, "")
                },
                outputArgs: null,
                referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // Read
            var mRead = this.entityBuilder.CreateAddMethodState(o, "Read",
                inputArgs: new Argument[] {
                    new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                    new Argument("Length", DataTypeIds.Int32, -1, "")
                },
                outputArgs: new Argument[] {
                    new Argument("Data", DataTypeIds.ByteString, -1, "")
                }, referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // Write
            var mWrite = this.entityBuilder.CreateAddMethodState(o, "Write",
                inputArgs: new Argument[] {
                    new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                    new Argument("Data", DataTypeIds.ByteString, -1, "")
                },
                outputArgs: null,
                referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // GetPosition
            var mGetPosition = this.entityBuilder.CreateAddMethodState(o, "GetPosition",
                inputArgs: new Argument[] {
                    new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                },
                outputArgs: new Argument[] {
                    new Argument("Position", DataTypeIds.UInt64, -1, "")
                },
                referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // SetPosition
            var mSetPosition = this.entityBuilder.CreateAddMethodState(o, "SetPosition",
                inputArgs: new Argument[] {
                    new Argument("FileHandle", DataTypeIds.UInt32, -1, ""),
                    new Argument("Position", DataTypeIds.UInt64, -1, "")
                },
                outputArgs: null,
                referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // result
            return o;
        }

        private ServiceResult OnReadCalled(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            var res = new ServiceResult(StatusCodes.Good);
            return res;
        }

        protected ServiceResult IsResumeExecutable(
            ISystemContext context,
            NodeState node,
            ref bool value)
        {
            value = true;
            return ServiceResult.Good;
        }

        protected ServiceResult IsResumeUserExecutable(
            ISystemContext context,
            NodeState node,
            ref bool value)
        {
            value = true;
            return ServiceResult.Good;
        }

    }
}
