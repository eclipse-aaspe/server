using AdminShellEvents;
using AdminShellNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AasxRestServerLibrary
{
    public class DeletedListItem
    {
        public AdminShell.Submodel sm;
        public AdminShell.Referable rf;
    }

    public class EventMessage
    {
        private AasPayloadStructuralChange changeClass = new AasPayloadStructuralChange();

        public List<DeletedListItem> DeletedList = new List<DeletedListItem>();

        public DateTime OlderDeletedTimeStamp = new DateTime();

        public void Add(AdminShell.Referable o, string op, AdminShell.Submodel rootSubmodel, ulong changeCount)
        {
            if (o is AdminShell.SubmodelElementCollection smec)
            {
                string json = "";

                AasPayloadStructuralChangeItem.ChangeReason reason = AasPayloadStructuralChangeItem.ChangeReason.Create;
                switch (op)
                {
                    case "Add":
                        reason = AasPayloadStructuralChangeItem.ChangeReason.Create;
                        json = JsonConvert.SerializeObject(smec, Newtonsoft.Json.Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                        break;
                    case "Remove":
                        reason = AasPayloadStructuralChangeItem.ChangeReason.Delete;
                        break;
                }

                rootSubmodel.SetAllParents();
                AdminShell.KeyList keys = new AdminShellV20.KeyList();

                // keys were in the reverse order
                keys = smec.GetReference()?.Keys;
                if (keys?.IsEmpty == false)
                    keys.Remove(keys.Last());

                AasPayloadStructuralChangeItem change = new AasPayloadStructuralChangeItem(
                    changeCount, o.TimeStamp, reason, keys, json);
                changeClass.Changes.Add(change);
                if (changeClass.Changes.Count > 100)
                    changeClass.Changes.RemoveAt(0);

                if (op == "Remove")
                {
                    o.TimeStamp = DateTime.Now;
                    AdminShell.Referable x = o;

                    DeletedList.Add(new DeletedListItem() { sm = rootSubmodel, rf = o });
                    if (DeletedList.Count > 1000 && DeletedList[0].rf != null)
                    {
                        OlderDeletedTimeStamp = DeletedList[0].rf.TimeStamp;
                        DeletedList.RemoveAt(0);
                    }
                }
            }
        }
    }
}
