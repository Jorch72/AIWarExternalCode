using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
    public class ExternalData_GroupTargetSorting : IArcenExternalDataPatternImplementation
    {
        public enum Items
        {
            Primitives,
            Length
        }

        public class Primitives
        {
            public bool IsCurrentlyTractoringMembersOfMyControlGroup;
        }

        public static int PatternIndex;
        private static string RelatedParentTypeName = "GameEntity";

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }

        public int GetNumberOfItems()
        {
            return (int)Items.Length;
        }

        public bool GetShouldInitializeOn( string ParentTypeName )
        {
            return ArcenStrings.Equals( ParentTypeName, RelatedParentTypeName );
        }

        public void InitializeData(object[] Target)
        {
            for ( Items itemEnum = 0; itemEnum < Items.Length; itemEnum++ )
            {
                object item = null;
                switch(itemEnum)
                {
                    case Items.Primitives:
                        item = new Primitives();
                        break;
                }
                Target[(int)itemEnum] = item;
            }
        }

        public void SerializeData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForSendingOverNetwork )
        {
            //unnecessary, this is all stuff that is not relevant between frames, let alone save/load
        }

        public void DeserializeData( object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsLoadingFromNetwork, GameVersion DeserializingFromGameVersion )
        {
            //unnecessary, this is all stuff that is not relevant between frames, let alone save/load
        }
    }

    public static class ExtensionMethodsFor__GroupTargetSorting
    {
        public static ExternalData_GroupTargetSorting.Primitives Get_GroupTargetSorting_Primitives( this GameEntity ParentObject)
        {
            return (ExternalData_GroupTargetSorting.Primitives)ParentObject.ExternalData.CollectionsByPatternIndex[ExternalData_GroupTargetSorting.PatternIndex].Data[(int)ExternalData_GroupTargetSorting.Items.Primitives];
        }
    }
}
