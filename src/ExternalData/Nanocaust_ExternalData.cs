using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
    public class NanocaustMgrData : IArcenExternalDataPatternImplementation
    {
        /* This stores the actual NanocaustMgr that's used by the Nanocaust faction
           We check out a copy for LongRangePlanning and PerSimStep and then save it again after
           each function. This is attached to a World object.

        Most of the actual serialization is done in the NanocaustMgr and FrenzyFleet classes. They each will
        export their data via a ToString() or Export() call, then that data is put into the SerializationBuffer here,
        and then there's a matching constructor that takes a String for NanocaustMgr and FrenzyFleet. */
        
        NanocaustMgr mgr;
        public static int PatternIndex;
        private static string RelatedParentTypeName = "World";
        public bool GetShouldInitializeOn( string ParentTypeName )
        {
            return ArcenStrings.Equals( ParentTypeName, RelatedParentTypeName );
        }
        public void ReceivePatternIndex( int Index )
        {
            //This gets a number assigned to it based off the ExternalData xml
            PatternIndex = Index;
        }
        public int GetNumberOfItems()
        {
            return 1; //the nanocaust manager object is the only thing that needs to be serialized to disk
        }

        public void InitializeData( object[] Target )
        {
            mgr = new NanocaustMgr();
            Target[0] = mgr;
        }

        public void SerializeData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForSendingOverNetwork )
        {
            NanocaustMgr thisMgr = (NanocaustMgr)Source[0];
            thisMgr.SerializeTo( Buffer, IsForSendingOverNetwork );
        }

        public void DeserializeData( object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsLoadingFromNetwork, GameVersion DeserializingFromGameVersion )
        {
            Target[0] = new NanocaustMgr( Buffer, IsLoadingFromNetwork, DeserializingFromGameVersion );
        }
    }

    public static class ExtensionMethodsFor_NanocaustMgrData
    {
        public static NanocaustMgr GetNanocaustMgr( this World ParentObject )
        {
            //The following is debug code from when I forgot to put this object in the XML.
            //Leaving it here in case my future self does something that stupid again
            // ArcenDebugging.ArcenDebugLogSingleLine( "entering GetNanocaustMgr", Verbosity.DoNotShow);
            // if(ParentObject == null)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "ParentObj is null", Verbosity.DoNotShow);
            //     return null;
            //   }
            // if(ParentObject.ExternalData == null)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "ExternalData is null", Verbosity.DoNotShow);
            //     return null;
            //   }
            // if(ParentObject.ExternalData.CollectionsByPatternIndex == null)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "Collections is null", Verbosity.DoNotShow);
            //     return null;
            //   }
            // ArcenDebugging.ArcenDebugLogSingleLine( "GetNanocaustMgr: about to check collections", Verbosity.DoNotShow);
            // if(ParentObject.ExternalData.CollectionsByPatternIndex == null)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "Collections is null", Verbosity.DoNotShow);
            //     return null;
            //   }
            // if(ParentObject.ExternalData.CollectionsByPatternIndex.Length < 1)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "Collections is empty", Verbosity.DoNotShow);
            //     return null;
            //   }

            // if(ParentObject.ExternalData.CollectionsByPatternIndex.Length < NanocaustMgrData.PatternIndex)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "Collections is too small (index " + NanocaustMgrData.PatternIndex + ")", Verbosity.DoNotShow);
            //     return null;
            //   }
            // ArcenDebugging.ArcenDebugLogSingleLine( "GetNanocaustMgr: about to check Data", Verbosity.DoNotShow);
            // if(ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustMgrData.PatternIndex].Data == null)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "Data is null", Verbosity.DoNotShow);
            //     return null;
            //   }
            // else
            //   ArcenDebugging.ArcenDebugLogSingleLine( "Data is not", Verbosity.DoNotShow);

            // if(ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustMgrData.PatternIndex].Data.Length < 1)
            //   {
            //     ArcenDebugging.ArcenDebugLogSingleLine( "Data is empty", Verbosity.DoNotShow);
            //     return null;
            //   }

            // // for(int i = 0; i < ParentObject.ExternalData.CollectionsByPatternIndex.Length; i++)
            // //   {
            // //     ArcenDebugging.ArcenDebugLogSingleLine( "loop type of object at collectionsByPatternIndex[ " +NanocaustMgrData.PatternIndex +"]: " + ParentObject.ExternalData.CollectionsByPatternIndex[i].Data[0].GetType(), Verbosity.DoNotShow);
            // //   }
            // ArcenDebugging.ArcenDebugLogSingleLine( "GetNanocaustMgr: data validated", Verbosity.DoNotShow);
            // if(ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustMgrData.PatternIndex].Data.Length == 0)
            //   return null;
            // if(ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustMgrData.PatternIndex].Data[0] == null)
            //   return null;
            return (NanocaustMgr)ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustMgrData.PatternIndex].Data[0];
        }

        public static void SetNanocaustMgr( this World ParentObject, NanocaustMgr mgr )
        {
            ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustMgrData.PatternIndex].Data[0] = mgr;
        }
    }

    public class NanocaustFleetID : IArcenExternalDataPatternImplementation
    {
        //This is an integer that's attached to a GameEntity to let the Entity know
        //which FrenzyFleet it is part of

        //The enum thing isn't really necessary for this one, it's pretty simple
        public enum Items
        {
            fleetIdentifier,
            Length
        }
        public int fleetID;
        public static int PatternIndex;
        public static string RelatedParentTypeName = "GameEntity"; //used for GameEntity objects

        public void ReceivePatternIndex( int Index )
        {
            PatternIndex = Index;
        }
        public int GetNumberOfItems()
        {
            //this could also just return 1
            return (int)Items.Length;
        }
        public bool GetShouldInitializeOn( string ParentTypeName )
        {
            //figure out which object type has this sort of ExternalData (in this case, a GameEntity)
            return ArcenStrings.Equals( ParentTypeName, RelatedParentTypeName );
        }

        public void InitializeData( object[] Target )
        {
            fleetID = -1;
            Target[0] = fleetID;
        }
        public void SerializeData( object[] Source, ArcenSerializationBuffer Buffer, bool IsForSendingOverNetwork )
        {
            //For saving to disk, translate this object into the buffer
            int id = (int)Source[0];
            Buffer.AddItem( id );
        }
        public void DeserializeData( object[] Target, int ItemsToExpect, ArcenDeserializationBuffer Buffer, bool IsLoadingFromNetwork, GameVersion DeserializingFromGameVersion )
        {
            //reverses SerializeData; gets the date out of the buffer and populates the variables
            fleetID = Buffer.ReadInt32();
            Target[0] = fleetID;
        }
    }

    public static class ExtensionMethodsFor_NanocaustFleetID
    {
        public static int GetNanocaustFleetID( this GameEntity ParentObject )
        {
            return (int)ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustFleetID.PatternIndex].Data[(int)NanocaustFleetID.Items.fleetIdentifier];
        }

        public static void SetNanocaustFleetId( this GameEntity ParentObject, int ID )
        {
            ParentObject.ExternalData.CollectionsByPatternIndex[NanocaustFleetID.PatternIndex].Data[(int)NanocaustFleetID.Items.fleetIdentifier] = ID;
        }
    }
}