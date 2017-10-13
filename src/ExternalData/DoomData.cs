using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
    public class DoomData : IArcenExternalDataPatternImplementation
    {
        public enum Items
        {
            /// <summary>
            /// Example of a collection of value-types grouped as a reference-type so that they can be accessed and set without boxing (which causes allocations on the heap at time of use rather than just time of creation)
            /// Also example of a field that is initialized to a non-null value for all instances
            /// </summary>
            Primitives,
            /// <summary>
            /// Example of a standalone reference-type field
            /// Also example of a field that is not initialized for all instances, and must be initialized before use
            /// </summary>
            DoomedPlanetIndices,
            Length
        }

        public class Primitives
        {
            public int SecondsUntilNextDoomAttack;
            public bool LastDoomAttackLaunchedAgainstPlayer;
            public int SecondsUntilNextDoomPlanetPick;
        }

        public static readonly bool DoDebugTestingLogic = false;
        public static int PatternIndex;
        private static string RelatedParentTypeName = "World";

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
                    case Items.DoomedPlanetIndices:
                        // not initialized on creation
                        break;
                }
                Target[(int)itemEnum] = item;
            }
        }

        public void SerializeData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForSendingOverNetwork )
        {
            for(Items itemEnum = 0;itemEnum < Items.Length;itemEnum++ )
            {
                object item = Source[(int)itemEnum];
                Buffer.AddItem( item != null );
                if ( item == null )
                    continue;
                switch(itemEnum)
                {
                    case Items.Primitives:
                        {
                            Primitives itemAsType = (Primitives)Source[(int)Items.Primitives];
                            Buffer.AddItem( itemAsType.SecondsUntilNextDoomAttack );
                            Buffer.AddItem( itemAsType.LastDoomAttackLaunchedAgainstPlayer );
                            Buffer.AddItem( itemAsType.SecondsUntilNextDoomPlanetPick );
                        }
                        break;
                    case Items.DoomedPlanetIndices:
                        {
                            List<int> itemAsType = (List<int>)Source[(int)Items.DoomedPlanetIndices];
                            Buffer.AddItem( itemAsType.Count );
                            for ( int i = 0; i < itemAsType.Count; i++ )
                                Buffer.AddItem( itemAsType[i] );
                        }
                        break;
                }
            }
        }

        public void DeserializeData( object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsLoadingFromNetwork, GameVersion DeserializingFromGameVersion )
        {
            for ( Items itemEnum = 0; itemEnum < Items.Length && (int)itemEnum < ItemsToExpect; itemEnum++ )
            {
                if ( !Buffer.ReadBool() )
                    continue;
                switch (itemEnum)
                {
                    case Items.Primitives:
                        {
                            Primitives item = ( Primitives)Target[(int)itemEnum];

                            item.SecondsUntilNextDoomAttack = Buffer.ReadInt32();
                            item.LastDoomAttackLaunchedAgainstPlayer = Buffer.ReadBool();
                            item.SecondsUntilNextDoomPlanetPick = Buffer.ReadInt32();
                        }
                        break;
                    case Items.DoomedPlanetIndices:
                        {
                            List<int> item = new List<int>();
                            Target[(int)Items.DoomedPlanetIndices] = item;

                            int countToExpect = Buffer.ReadInt32();
                            for ( int i = 0; i < countToExpect; i++ )
                                item.Add( Buffer.ReadInt32() );
                        }
                        break;
                }
            }
        }
    }

    public static class ExtensionMethodsFor_DoomData
    {
        public static DoomData.Primitives GetDoomData_Primitives(this World ParentObject)
        {
            return (DoomData.Primitives)ParentObject.ExternalData.CollectionsByPatternIndex[DoomData.PatternIndex].Data[(int)DoomData.Items.Primitives];
        }

        public static List<int> GetDoomData_DoomedPlanetIndices( this World ParentObject )
        {
            return (List<int>)ParentObject.ExternalData.CollectionsByPatternIndex[DoomData.PatternIndex].Data[(int)DoomData.Items.DoomedPlanetIndices];
        }

        public static void SetDoomData_DoomedPlanetIndices( this World ParentObject, List<int> Item )
        {
            ParentObject.ExternalData.CollectionsByPatternIndex[DoomData.PatternIndex].Data[(int)DoomData.Items.DoomedPlanetIndices] = new List<int>();
        }
    }
}
