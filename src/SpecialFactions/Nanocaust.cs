using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /* Here are the tunables/globals for the Nanocaust faction.
             It is intended that all tuning of the Nanocaust could just be done here
             with no further code modification. I'd like to eventually put critical values
             into the XML so we can tune it without recompiling */
    public class NanocaustMgr
    {
        /* Reminder for modders: FInt is a fixed point integer. Arcen uses it
           because floating point math isn't always consistent across OS/Arch for multiplayer.
           It is critical not to use Floating point values in sim code. Always use FIint
           To create one, say FInt.FromParts( <portion greater than 1>, <decimal portion> )
           So FInt.FromParts(0, 500) == 0.5 and FInt.FromParts(100, 200) == 100.2
           Best practice suggests always making the decimal portion precisely 3 digits. */

        public bool debug = false;
        public bool hasBeenHacked = false;
        public Planet hivePlanet = null;


        public bool hasSimStepRun = false; //For the reload case, if you reload and then the DoLongRangePlanning code runs before
                                           //PerSimStep code then we can have problems.
                                           /* Timing related fields. All times are in seconds */

        //The initialization value is the first time a frenzy happens. This value is updated
        //after each frenzy to know when to go next
        public int timeForNextFrenzy;
        public int nanobotLifespan;

        //The progress modification code is a bit of a hack, but it's intended as a workaround until Keith can work
        //his magic on things
        public int progressModification; //every time the nanocaust takes a planet, we need to adjust the progress a bit to take
                                         //that into account (otherwise the nanocaust jacks the progress up really high
        public List<Planet> totalPlanetsEverTaken = new List<Planet>(); //we track the list of all the planets the Nanocaust has conquered
                                                                        //because if the nanocaust loses planets we still want to adjust the progress down
                                                                        //For example, if a planet is taken by the nanocaust and then the Devourer destroys the
                                                                        //nanobot center...

        public int modifierMaxStrengthPerPlanetForFrenzy;
        //in case we want to make the next frenzy sooner (for example,
        //if the nanobots lose a planet we might want them to frenzy sooner)
        //Making this negative makes the next frenzy sooner, positive makes it come later
        public int timeModifierForNextFrenzy;

        /* These lists are used throughout the code to keep track of state */
        public readonly List<Planet> nanobotCenterPlanets = new List<Planet>(); //planets with nanobot centers
                                                                                //these three lists are currently unused; they are intended for when we are doing periodic raids against
                                                                                //adjoining, non-nanocaust planets. This code hasn't been adapted to use fleets yet
        public readonly List<Planet> quiescedPlanets = new List<Planet>(); //planets with a nanobot center and no uninfectedPlanets adjoining it
        public readonly List<Planet> activePlanets = new List<Planet>(); //planets with a nanobot center and uninfectedPlanets adjoining it
        public readonly List<Planet> uninfectedPlanets = new List<Planet>(); //planets without a nanobot center adjoining a planet with a nanobot center
                                                                             //this list is used to select potential targets for attacking.
                                                                             //having duplicate entries on this list is fine (it increases the likelihood
                                                                             //of attacking that planet

        public bool humanVision = false; //have the humans spotted the Nanocaust yet?
        public bool humanEncounter = false; //have the humans and the nanocaust started fighting yet? Currently unused
        public List<FrenzyFleet> fleets = new List<FrenzyFleet>();
        public int numFleetsSent = -1;

        public NanocaustMgr()
        {
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "NanocaustMgrConstructor(): <no arguments>", Verbosity.DoNotShow );

            CustomDataSet nanocaustConstants = ExternalConstants.Instance.GetCustomData( "nanocaust" );
            timeForNextFrenzy = nanocaustConstants.GetInt( "timeForFirstFrenzy" );

            //clear all the lists
            nanobotCenterPlanets.Clear();
            activePlanets.Clear();
            quiescedPlanets.Clear();
            uninfectedPlanets.Clear();
            fleets.Clear();
            totalPlanetsEverTaken.Clear();

            //set all the variables to default values
            hasBeenHacked = false;
            hivePlanet = null;
            modifierMaxStrengthPerPlanetForFrenzy = 0;
            timeModifierForNextFrenzy = 0;
            humanVision = false;
            humanEncounter = false;
            numFleetsSent = 0;
            progressModification = 0;
            hasSimStepRun = false;
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForSendingOverNetwork )
        {
            //Things that need to get serialized: Fleet objects. time and strength modifiers.
            //hasHumanVision, humanEncounter, progressModification, totalPlanetsEverTaken*/
            Buffer.AddItem( humanVision );
            Buffer.AddItem( humanEncounter );
            Buffer.AddItem( progressModification );

            Buffer.AddItem( totalPlanetsEverTaken.Count );
            for ( int i = 0; i < totalPlanetsEverTaken.Count; i++ )
                Buffer.AddItem( totalPlanetsEverTaken[i].PlanetIndex );

            Buffer.AddItem( fleets.Count );
            for ( int i = 0; i < fleets.Count; i++ )
                fleets[i].SerializeTo( Buffer, IsForSendingOverNetwork );

            Buffer.AddItem( numFleetsSent );
            Buffer.AddItem( timeForNextFrenzy );
            Buffer.AddItem( modifierMaxStrengthPerPlanetForFrenzy );
            Buffer.AddItem( timeModifierForNextFrenzy );
        }

        public NanocaustMgr( ArcenDeserializationBuffer Buffer, bool IsLoadingFromNetwork, GameVersion DeserializingFromGameVersion )
        {
            hasSimStepRun = false;

            this.humanVision = Buffer.ReadBool();
            this.humanEncounter = Buffer.ReadBool();
            this.progressModification = Buffer.ReadInt32();

            int countToExpect;

            countToExpect = Buffer.ReadInt32();
            for ( int i = 0; i < countToExpect; i++ )
                totalPlanetsEverTaken.Add( World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt32() ) );

            countToExpect = Buffer.ReadInt32();
            for ( int i = 0; i < countToExpect; i++ )
                fleets.Add( new FrenzyFleet( Buffer, IsLoadingFromNetwork, DeserializingFromGameVersion ) );

            this.numFleetsSent = Buffer.ReadInt32();
            this.timeForNextFrenzy = Buffer.ReadInt32();
            this.modifierMaxStrengthPerPlanetForFrenzy = Buffer.ReadInt32();
            this.timeModifierForNextFrenzy = Buffer.ReadInt32();
        }
    }
    /* The Nanocaust goes on the offensive by Frenzying.
       When the PerSimStep code wants to attack it will create a FrenzyFleet
       and add it to the NanocaustMgr's "fleets" structure.
       It sets how strong of an attack to make and where to strike.
       Then the LongRangePlanning code builds a fleet and runs things independently.
       This allows the Nanocaust to do multiple things simultaneously in a well-scaling fashion.
       The one confusion is if multiple FrenzyFleets have the same target or staging area
       then it can get confusing. I'd love to be able to let each GameEntity know that it's part of
       a particular fleet (this would let me do things like have patrols and so on), but that's
       for Keith */
    public class FrenzyFleet
    {
        public readonly List<GameEntity> ships = new List<GameEntity>();
        public Planet stagingArea; //the ships all meet at the stagingArea before setting off on an attack
        public Planet target; //the eventual target of the fleet
        public FrenzyGoal goal; //What the fleet is doing; see the FrenzyGoal enum

        //two ways to limit fleet strength, either total overall
        //or per planet. Only use one
        public int maxStrengthForFleet;
        public int maxStrengthPerPlanetForFleet;

        //We need a small state machine for each fleet
        public bool attackingFleet;
        public bool startStaging;
        public bool stillStaging;
        public bool creatingFleet;
        public bool fleetTriumphant;
        public bool fleetDefeated;


        public int time; //The time (in Seconds Since Game Start) that the fleet was kicked off
        public int fleetId; //unique ID for a fleet
        
        public FrenzyFleet( Planet target, Planet stagingArea, FrenzyGoal goal )
        {
            maxStrengthPerPlanetForFleet = -1;
            maxStrengthForFleet = -1;
            ships = new List<GameEntity>(); ;
            time = -1;
            fleetId = -2;
            startStaging = false;
            stillStaging = false;
            attackingFleet = false;
            creatingFleet = true;
            fleetTriumphant = false;
            fleetDefeated = false;

            this.target = target;
            this.stagingArea = stagingArea;
            this.target = target;
            this.goal = goal;
        }
        public FrenzyFleet( ArcenDeserializationBuffer Buffer, bool IsLoadingFromNetwork, GameVersion DeserializingFromGameVersion )
        {
            ships = new List<GameEntity>();

            this.target = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt32() );
            this.stagingArea = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt32() );
            this.time = Buffer.ReadInt32();
            this.fleetId = Buffer.ReadInt32();
            this.goal = (FrenzyGoal)Buffer.ReadInt32();
            this.startStaging = Buffer.ReadBool();
            this.stillStaging = Buffer.ReadBool();
            this.attackingFleet = Buffer.ReadBool();
            this.creatingFleet = Buffer.ReadBool();
            this.fleetTriumphant = Buffer.ReadBool();
            this.fleetDefeated = Buffer.ReadBool();
        }

        public void SerializeTo( ArcenSerializationBuffer Buffer, bool IsForSendingOverNetwork )
        {
            //for Serialize/Deserialize, we need to export/import all the data
            //Since we are going to using the NanocaustMgr as the unit of serialize/deserialize,
            //this class does not explicitly do it as well; instead the NanocaustMgr will call Export() for each fleet.
            //. I could imagine wanting a FrenzyFleet to eventually handle Serialize/Deserialize though

            //Note that for Export/Import, we export the planet Index (and then use GetPlanetByIndex)
            Buffer.AddItem( target == null ? -1 : target.PlanetIndex );
            Buffer.AddItem( stagingArea == null ? -1 : stagingArea.PlanetIndex );
            Buffer.AddItem( time );
            Buffer.AddItem( fleetId );
            Buffer.AddItem( (int)goal );
            Buffer.AddItem( startStaging );
            Buffer.AddItem( stillStaging );
            Buffer.AddItem( attackingFleet );
            Buffer.AddItem( creatingFleet );
            Buffer.AddItem( fleetTriumphant );
            Buffer.AddItem( fleetDefeated );
        }

        public override string ToString()
        {
            //ToString is for printing/logging.
            string state = "Unknown";
            if ( this.creatingFleet )
                state = "Creating";

            if ( this.attackingFleet )
                state = "Attacking " + target.Name;
            if ( this.stillStaging )
                state = "StillStaging " + stagingArea.Name + " --> " + target.Name;
            if ( this.startStaging )
                state = "StartStaging " + stagingArea.Name + " --> " + target.Name;
            string goalStr = this.goal.ToString();

            string output = "Fleet " + fleetId + " <" + this.time + "> " + state + " ==> " + goalStr;
            return output;
        }
    }
    public enum FrenzyGoal
    {
        //Not all Frenzy types are allowed to capture a planet.
        //Currently this is done for balance reasons. So if
        //if you add a new frenzy you want to be able to capture planets,
        //you need to add a reference to it in addConstructorsToConqueredPlanets
        Unset = 0, //initialization value
        ExpandRandom, //random target
        ExpandOutwardFromHive, //picks the closest planet to the Hive
        ExpandWeakest,  //expend to the weakest target
        ExpandTowardHuman,
        ExpandTowardAI,
        HumanKing, //try to kill the human king
        AIKing, //try to kill the AI king
        Chaos, //currently not used, but it will be!
        Patrol, //patrols. Duh. Not currently used
    }

    public class SpecialFaction_Nanocaust : ISpecialFactionImplementation
    {
        public static SpecialFaction_Nanocaust Instance;
        public SpecialFaction_Nanocaust() { Instance = this; }

        public static readonly string NANOCAUST_TAG = "NanobotCenter"; //all nanobot centers match this
        public static readonly string NANOCAUST_HIVE = "NanobotHive"; //this is for the initial seeding
        public static readonly string NANOCAUST_HACKED_HIVE = "NanobotHackedHive"; //this is for the initial seeding

        /* Modding note: For tags, they are set in the XML as comma seperated fields (tags="field1,field2")
           then you can reference these via the GameEntity.TypeData.Tags, which is a List<string> of the different fields.
           You then can use GetRandomWithTag or GetFirstWithTag and so on. Very handy. */


        //The NanocaustMgr is attached to the World object via ExternalData. It's null here
        //and we call Get before PerSimStep and LongRangePlanning
        public NanocaustMgr mgr = null;

        public void SetStartingSideRelationships( WorldSide side )
        {
            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide otherSide = World_AIW2.Instance.Sides[i];
                if ( side == otherSide )
                    continue;
                switch ( otherSide.Type )
                {
                    case WorldSideType.Player:
                    case WorldSideType.AI:
                    case WorldSideType.SpecialFaction:
                        side.MakeHostileTo( otherSide );
                        otherSide.MakeHostileTo( side );
                        break;
                }
            }
        }
        public ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side )
        {
            ArcenEnumIndexedArray_AIBudgetType<FInt> result = new ArcenEnumIndexedArray_AIBudgetType<FInt>();

            result[AIBudgetType.Reinforcement] = FInt.One;

            return result;
        }

        public bool GetShouldAttackNormallyExcludedTarget( WorldSide side, GameEntity Target )
        {
            //This function is apparently called before the game starts, and the mgr isn't initialized
            //until the first PerSimStep
            if ( mgr == null )
                return false;
            //if the Nanobots control a planet, destroy the warp gate and controller
            Planet planet = Target.Combat.Planet;
            if ( !mgr.nanobotCenterPlanets.Contains( planet ) )
                return false;

            if ( Target.TypeData.SpecialType == SpecialEntityType.Controller )
                return true;
            if ( ArcenStrings.ListContains( Target.TypeData.Tags, "WarpGate" ) )
                return true;

            return false;
        }

        public void SeedStartingEntities( WorldSide side, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
            galaxy.Mapgen_SeedSpecialEntities( Context, side, NANOCAUST_HIVE, 1 );
        }

        /* This function is used to determine where your ships should go */
        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
            bool localDebug = false;
            bool veryVerboseDebug = false;

            if ( World.Instance.GetNanocaustMgr() == null )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "manager uninitialized, so don't do any LongRangePlanning until it gets initialized", Verbosity.DoNotShow );
                return;
            }
            //Note that I'm setting the global mgr here. This seems to work fine.
            //I think DoLongRangePlanning and PerSim are called by different threads, so best to make sure we have
            //an up to date copy for the global at all time.
            mgr = World.Instance.GetNanocaustMgr();

            ArcenSparseLookup<Planet, List<GameEntity>> unassignedThreatShipsByPlanet = new ArcenSparseLookup<Planet, List<GameEntity>>();
            //List<GameEntity> frenzyFleet = new List<GameEntity>();
            if ( veryVerboseDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "*********************", Verbosity.DoNotShow );

            if ( !mgr.hasSimStepRun )
            {
                //If we haven't run through the PerSimStep code then some fields won't be initialized in the Manager
                if ( veryVerboseDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "PerSimStep code hasn't run, which suggests you've reloaded", Verbosity.DoNotShow );
                return;
            }
            if ( veryVerboseDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "I have a manager", Verbosity.DoNotShow );

            for ( int i = 0; i < mgr.fleets.Count; i++ )
            {
                if ( veryVerboseDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "resetting ships for fleet " + i + " of " + mgr.fleets.Count, Verbosity.DoNotShow );

                FrenzyFleet fleet = mgr.fleets[i];
                fleet.ships.Clear();
                mgr.fleets[i] = fleet;
            }

            /* this delegate finds all unassigned ships */
            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( entity.LongRangePlanningData == null )
                    return DelReturn.Continue; // if created after the start of this planning cycle, skip

                Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex );
                if ( entity.TypeData.GetHasTag( NANOCAUST_TAG ) )
                {
                    // a nanobot center of any sort (including the Hive)
                    return DelReturn.Continue;
                }
                else
                {

                    if ( veryVerboseDebug && entity.TypeData != null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Found ship " + entity.TypeData.Name + " on " + planet.Name, Verbosity.DoNotShow );

                    //Each ship in the Nanocaust has an extra field attached to it via ExternalData
                    //to let us keep track of which FrenzyFleet it's attached to (if any)
                    int fleetIdForThisShip = entity.GetNanocaustFleetID();
                    if ( fleetIdForThisShip == -1 )
                    {
                        //this ship is not part of a fleet
                        //Nanobots have finite lifespans, but they only die once they
                        //aren't busy
                        if ( entity.SecondsSinceCreation > mgr.nanobotLifespan )
                        {
                            if ( localDebug )
                            {
                                if ( veryVerboseDebug && entity.LongRangePlanningData.CurrentPlanetIndex == -1 )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine( "ship on UNKNOWN dies now (age " + entity.SecondsSinceCreation + ")", Verbosity.DoNotShow );
                                }
                                else if ( veryVerboseDebug && entity.LongRangePlanningData.FinalDestinationPlanetIndex == -1 )
                                {
                                    //I'm not sure when the FinalDestinationPlanetIndex is allowed to be -1,
                                    //but per Keith's code it is.
                                    ArcenDebugging.ArcenDebugLogSingleLine( "ship on " + World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex ).Name + " going to  UNKNOWN dies now (age " + entity.SecondsSinceCreation + ")", Verbosity.DoNotShow );
                                }
                                else if ( veryVerboseDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "ship on " + World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex ).Name + " going to " + World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.FinalDestinationPlanetIndex ).Name + "dies now (age " + entity.SecondsSinceCreation + ")", Verbosity.DoNotShow );
                            }
                            entity.Die( Context );
                            return DelReturn.Continue;
                        }
                        if ( !unassignedThreatShipsByPlanet.GetHasKey( planet ) )
                            unassignedThreatShipsByPlanet[planet] = new List<GameEntity>();
                        unassignedThreatShipsByPlanet[planet].Add( entity );
                        if ( veryVerboseDebug && entity.TypeData != null )
                            ArcenDebugging.ArcenDebugLogSingleLine( " Ship is unassigned", Verbosity.DoNotShow );
                        return DelReturn.Continue;
                    }
                    else
                    {
                        //This ship is in a fleet, so figure out which fleet
                        for ( int i = 0; i < mgr.fleets.Count; i++ )
                        {
                            FrenzyFleet fleet = mgr.fleets[i];
                            if ( fleet.fleetId == fleetIdForThisShip )
                            {
                                //We found which fleet this ship is in
                                entity.EntitySpecificOrders.Behavior = EntityBehaviorType.Attacker;
                                fleet.ships.Add( entity );

                                if ( entity.LongRangePlanningData.FinalDestinationPlanetIndex != -1 &&
                                     entity.LongRangePlanningData.FinalDestinationPlanetIndex != entity.LongRangePlanningData.CurrentPlanetIndex )
                                {
                                    if ( veryVerboseDebug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( " Ship is in fleet " + fleetIdForThisShip + " from time " + fleet.time + " and is still staging", Verbosity.DoNotShow );
                                    //if we are still in the Staging phase, signal that we are still staging ships to the
                                    //stagingPlanet before beginning the attack
                                    if ( fleet.startStaging )
                                        fleet.stillStaging = true;
                                }
                                if ( fleet.startStaging && ( entity.LongRangePlanningData.CurrentPlanetIndex == fleet.stagingArea.PlanetIndex ) && veryVerboseDebug && entity.TypeData != null )
                                    ArcenDebugging.ArcenDebugLogSingleLine( " Ship in fleet " + fleetIdForThisShip + " reached its stage point", Verbosity.DoNotShow );
                                if ( fleet.attackingFleet && ( entity.LongRangePlanningData.CurrentPlanetIndex == fleet.target.PlanetIndex ) && veryVerboseDebug && entity.TypeData != null )
                                    ArcenDebugging.ArcenDebugLogSingleLine( " Ship in fleet " + fleetIdForThisShip + " is fighting at the attack point", Verbosity.DoNotShow );
                                else if ( fleet.attackingFleet && veryVerboseDebug && entity.TypeData != null )
                                    ArcenDebugging.ArcenDebugLogSingleLine( " Ship is in fleet " + fleetIdForThisShip + " and is at " + planet.Name + " which is not the attack target", Verbosity.DoNotShow );

                                mgr.fleets[i] = fleet;

                                return DelReturn.Continue;
                            }
                        }
                        ArcenDebugging.ArcenDebugLogSingleLine( " BUG: ship is attached to unknown fleet " + fleetIdForThisShip, Verbosity.DoNotShow );
                        return DelReturn.Continue;
                    }
                }
            } );

            /* Now that the ships have been processed (part of a FrenzyFleet or unassigned) */

            int pairCount = unassignedThreatShipsByPlanet.GetPairCount();
            //Handle frenzy fleets first
            for ( int i = 0; i < mgr.fleets.Count; i++ )
            {
                //TODO: I can probably replace a lot of this logging code with calls to FrenzyFleet.ToString()
                FrenzyFleet fleet = mgr.fleets[i];
                if ( fleet.startStaging && fleet.stillStaging )
                {
                    //Ships are still heading to the staging point. reset stillStaging for the check next iteration
                    fleet.stillStaging = false;
                    if ( localDebug && ( World_AIW2.Instance.GameSecond % 10 == 0 ) && World_AIW2.Instance.IsFirstFrameOfSecond )
                        ArcenDebugging.ArcenDebugLogSingleLine( "fleet " + fleet.fleetId + " fleet staging " + fleet.stagingArea.Name + " --> " + fleet.target.Name, Verbosity.DoNotShow );

                }
                else if ( fleet.startStaging && !fleet.stillStaging )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "fleet " + fleet.fleetId + " fleet has fully staged to " + fleet.stagingArea.Name + ". Now attack " + fleet.target.Name, Verbosity.DoNotShow );
                    fleet.startStaging = false;
                    fleet.stillStaging = false;
                    fleet.attackingFleet = true;
                    NanocaustUtilityMethods.Helper_RaidSpecificPlanet( fleet.ships, fleet.stagingArea, side, World_AIW2.Instance.SetOfGalaxies.Galaxies[0], fleet.target, false, Context );
                }
                else if ( fleet.creatingFleet )
                {
                    //The PerSimStep code has signalled the start of a new FrenzyFleet,
                    //so assign it ships and set it in motion
                    Planet destPlanet;
                    if ( fleet.stagingArea == null )
                        destPlanet = fleet.target;
                    else
                        destPlanet = fleet.stagingArea;
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Fleet dispatched at " + fleet.time + "; Creating fleet. MaxStrengthForFleet " + fleet.maxStrengthForFleet + " maxPerPlanet " + fleet.maxStrengthPerPlanetForFleet, Verbosity.DoNotShow ); //-1 means "use the strength value"

                    int totalStrengthSoFar = 0;
                    List<GameEntity> shipsToSendForThisPlanet = new List<GameEntity>();
                    for ( int j = 0; j < pairCount; j++ )
                    {
                        shipsToSendForThisPlanet.Clear();
                        ArcenSparseLookupPair<Planet, List<GameEntity>> pair = unassignedThreatShipsByPlanet.GetPairByIndex( j );
                        int strengthForPlanetSoFar = 0;
                        Planet planet = pair.Key;
                        List<GameEntity> shipsOnPlanet = pair.Value;
                        if ( veryVerboseDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Drawing ships from " + planet.Name + " (" + j + "/" + pairCount + ") totalShips on planet " + shipsOnPlanet.Count + " str " + NanocaustUtilityMethods.strengthOfList( shipsOnPlanet ), Verbosity.DoNotShow );
                        bool shipsLeftToExamine = true;
                        int index = 0;
                        while ( shipsLeftToExamine && shipsOnPlanet.Count > 0 )
                        {
                            //Check each of the ships on the plant
                            FInt strengthOfSquad = NanocaustUtilityMethods.strengthOfEntity( shipsOnPlanet[index] );
                            shipsToSendForThisPlanet.Add( shipsOnPlanet[index] );
                            shipsOnPlanet[index].SetNanocaustFleetId( fleet.fleetId );
                            shipsOnPlanet.Remove( shipsOnPlanet[index] );
                            totalStrengthSoFar += (int)strengthOfSquad;
                            strengthForPlanetSoFar += (int)strengthOfSquad;
                            if ( fleet.maxStrengthPerPlanetForFleet != -1 ) //-1 means "ignore"
                            {
                                if ( strengthForPlanetSoFar >= fleet.maxStrengthPerPlanetForFleet )
                                {
                                    if ( localDebug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Name + " hit limit (strength pre planet) ", Verbosity.DoNotShow );
                                    shipsLeftToExamine = false;
                                }
                            }
                            else if ( fleet.maxStrengthForFleet != -1 )
                            {
                                if ( totalStrengthSoFar >= fleet.maxStrengthForFleet )
                                {
                                    if ( localDebug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Name + " hit limit (total) ", Verbosity.DoNotShow );
                                    //we have taken as many ships from this planet as we need
                                    shipsLeftToExamine = false;
                                }
                            }
                            else
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no selection method for fleet strength given", Verbosity.DoNotShow );
                            }
                        }
                        //Now send the ships chosen from this planet
                        if ( veryVerboseDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Stage " + fleet.time + " from " + planet.Name + " to " + destPlanet.Name + " numships " + shipsToSendForThisPlanet.Count + " str " + NanocaustUtilityMethods.strengthOfList( shipsToSendForThisPlanet ), Verbosity.DoNotShow );
                        NanocaustUtilityMethods.Helper_RaidSpecificPlanet( shipsToSendForThisPlanet, planet, side, World_AIW2.Instance.SetOfGalaxies.Galaxies[0], destPlanet, false, Context );
                        if ( fleet.maxStrengthForFleet != -1 && totalStrengthSoFar >= fleet.maxStrengthForFleet )
                        {
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "frenzy fleet hit total strength limit ", Verbosity.DoNotShow );

                            break;
                        }
                    }
                    //Now we have the fleet
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Fleet dispatched", Verbosity.DoNotShow );
                    fleet.creatingFleet = false;
                    fleet.startStaging = true;
                }
                else if ( fleet.attackingFleet )
                {
                    if ( fleet.goal == FrenzyGoal.HumanKing )
                    {
                        //update target to match human king location. Note this behaviour
                        //is currently untested, but noone has gotten far enough into the
                        //game yet to make it relevant ;-)
                        Planet kingPlanet = NanocaustUtilityMethods.findHumanKing();
                        if ( kingPlanet == null )
                        {
                            //the human king seems to be dead? So the game is over, right?
                            fleet.fleetTriumphant = true;
                        }
                        if ( kingPlanet != fleet.target )
                        {
                            //This behaviour is currently vulnerable to a defender moving the ark rapidly
                            //between planets to pull the Nanocaust force through defenses. This is probably desirable
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Human king moved.  " + fleet.target.Name + " --> " + kingPlanet.Name, Verbosity.DoNotShow );
                            fleet.attackingFleet = false;
                            fleet.startStaging = true;

                            fleet.stagingArea = fleet.target;
                            fleet.target = kingPlanet;
                        }
                    }
                    if ( fleet.goal == FrenzyGoal.AIKing )
                    {
                        //update target to match AI king location (Stage 2 MasterController can move)
                        //Note this behaviour is currently untested
                        Planet kingPlanet = NanocaustUtilityMethods.findAIKing();
                        if ( kingPlanet == null )
                        {
                            //the AI king seems to be dead? So the game is over, right?
                            fleet.fleetTriumphant = true;
                        }
                        if ( kingPlanet != fleet.target )
                        {
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "AI king moved.  " + fleet.target.Name + " --> " + kingPlanet.Name, Verbosity.DoNotShow );
                            fleet.attackingFleet = false;
                            fleet.startStaging = true;

                            fleet.stagingArea = fleet.target;
                            fleet.target = kingPlanet;
                        }
                    }
                    //check how much of the attacking fleet is left to see if we were defeated
                    FInt currentFleetStrength = NanocaustUtilityMethods.strengthOfList( fleet.ships );
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "fleet " + fleet.fleetId + " numShips: " + fleet.ships.Count + "  attacking " + fleet.target.Name + " strength  " + currentFleetStrength, Verbosity.DoNotShow );
                    if ( fleet.ships.Count == 0 )
                    {
                        if ( localDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "fleet " + fleet.fleetId + " defeated ", Verbosity.DoNotShow );
                        fleet.fleetDefeated = true;
                    }

                    //If the PerSimStep code put a Nanocaust Center on the planet (which happens after we win a battle)
                    if ( NanocaustUtilityMethods.isPlanetOnList( mgr.nanobotCenterPlanets, fleet.target ) )
                    {
                        if ( localDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "fleet " + fleet.fleetId + " triumphant ", Verbosity.DoNotShow );
                        fleet.fleetTriumphant = true;
                    }
                }
                //fleet isn't a pointer, so we need copy the updated values back
                mgr.fleets[i] = fleet;
            }
            World.Instance.SetNanocaustMgr( mgr ); //The NanocaustMgr is attached to the World object via ExternalData,
                                                   //so update it with the changes from LongRangePlanning

        }

        /* This is used to update Faction State */
        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
            bool localDebug = false;

            if ( !World_AIW2.Instance.IsFirstFrameOfSecond )
                return;
            //We pull some constants from ExternalConstants (Nanocaust.xml)
            CustomDataSet nanocaustConstants = ExternalConstants.Instance.GetCustomData( "nanocaust" );
            int secondsBetweenSimUpdates = nanocaustConstants.GetInt( "secondsBetweenNanocaustSimUpdates" ); ;

            //Get the most recent copy of the NanocaustMgr (this might be the first simStep after a reload)
            if ( World.Instance.GetNanocaustMgr() == null )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "initializing nanocaust mgr", Verbosity.DoNotShow );
                World.Instance.SetNanocaustMgr( new NanocaustMgr() );
            }
            mgr = World.Instance.GetNanocaustMgr();

            if ( ( World_AIW2.Instance.GameSecond % secondsBetweenSimUpdates == 0 ) ||
                 World_AIW2.Instance.GameSecond == 1 || !mgr.hasSimStepRun ) //Run this every secondsBetweenSimUpdates seconds, and the very first second
            {
                int planetsForEarlyExpansion = nanocaustConstants.GetInt( "planetsForEarlyExpansion" ); ;
                int baseTimeBetweenFrenzies = nanocaustConstants.GetInt( "baseTimeBetweenFrenzies" ); ;
                int minTimeBetweenFrenzies = nanocaustConstants.GetInt( "minTimeBetweenFrenzies" ); ;
                int baseMaxStrengthPerPlanetForFrenzy = nanocaustConstants.GetInt( "baseMaxStrengthPerPlanetForFrenzy" ); ;

                if ( mgr.hasBeenHacked )
                {
                    //find the Hive and replace it with a Hacked Hive
                    //This makes the Nanocaust into allies of the humans and
                    //will now make them Frenzy against the AI
                    NanocaustUtilityMethods.makeFriendsWithHumans( side );
                }
                //update the various planet State lists
                findPlanetsWithConstructors( side, mgr.nanobotCenterPlanets, true );
                if ( mgr.nanobotCenterPlanets.Count == 0 || mgr.hivePlanet == null )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( " Nanocaust defeated\n", Verbosity.DoNotShow );
                    return;
                }

                updatePlanetLists();
                //on the very first SimUpdate we run, strengthen the Nanocaust Hive(s)
                //to make sure we get off to a good start
                if ( World_AIW2.Instance.GameSecond <= secondsBetweenSimUpdates )
                    doInitialPlanetStuff( side, Context );

                //DEBUG LOGGING
                if ( localDebug && World_AIW2.Instance.GameSecond % 90 == 0 )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( " Logging planet lists (PerSimStep) at " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                    // ArcenDebugging.ArcenDebugLogSingleLine( " total planets ever captured " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                    //  for ( int i = 0; i < mgr.totalPlanetsEverTaken.Count; i++ )
                    //    ArcenDebugging.ArcenDebugLogSingleLine( " taken:  " + mgr.totalPlanetsEverTaken[i].Name, Verbosity.DoNotShow );
                    //ArcenDebugging.ArcenDebugLogSingleLine( " Logging planet lists (PerSimStep) at " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                    // for ( int i = 0; i < mgr.nanobotCenterPlanets.Count; i++ )
                    //   ArcenDebugging.ArcenDebugLogSingleLine( " nanobotCenter  " + mgr.nanobotCenterPlanets[i].Name, Verbosity.DoNotShow );
                    // for ( int i = 0; i < mgr.quiescedPlanets.Count; i++ )
                    //   ArcenDebugging.ArcenDebugLogSingleLine( " quiesced  " + mgr.quiescedPlanets[i].Name, Verbosity.DoNotShow );
                    // for ( int i = 0; i < mgr.activePlanets.Count; i++ )
                    //   ArcenDebugging.ArcenDebugLogSingleLine( " activePlanets  " + mgr.activePlanets[i].Name, Verbosity.DoNotShow );
                    // for ( int i = 0; i < mgr.uninfectedPlanets.Count; i++ )
                    //   ArcenDebugging.ArcenDebugLogSingleLine( " uninfectedPlanets  " + mgr.uninfectedPlanets[i].Name, Verbosity.DoNotShow );
                }
                //END DEBUG

                //Update our currently existing fleets
                //Also if a fleet has triumphed or been defeated, then
                //modify the "Create Next Frenzy" parameters accordingly
                for ( int i = 0; i < mgr.fleets.Count; i++ )
                {
                    bool deleteFleet = false;
                    FrenzyFleet fleet = mgr.fleets[i];
                    if ( fleet.fleetTriumphant )
                    {
                        deleteFleet = true;
                        //reset the modifiers on a Triumph
                        mgr.modifierMaxStrengthPerPlanetForFrenzy = 0;
                        mgr.timeModifierForNextFrenzy = 0;
                        //frenzies will come no more frequently than minTimeBetweenWaves
                        //if we are below the minPlanetsForLimiting, make it come at that interval so
                        //the nanocaust quickly gets up to strength
                        if ( baseTimeBetweenFrenzies + mgr.timeModifierForNextFrenzy < minTimeBetweenFrenzies )
                            mgr.timeModifierForNextFrenzy = minTimeBetweenFrenzies - baseTimeBetweenFrenzies;
                        if ( mgr.nanobotCenterPlanets.Count < planetsForEarlyExpansion )
                            mgr.timeModifierForNextFrenzy = minTimeBetweenFrenzies - baseTimeBetweenFrenzies;
                    }
                    if ( fleet.fleetDefeated )
                    {
                        deleteFleet = true;
                        mgr.modifierMaxStrengthPerPlanetForFrenzy += 500; //make the next wave stronger
                                                                          //I hate ternary operators, hence the following syntax
                        mgr.timeModifierForNextFrenzy -= 60;
                        //frenzies will come no more frequently than minTimeBetweenWaves
                        //if we have fewer planets than planetsForEarlyExpansion, send extra strong
                        //waves and always take weak targets so the nanocaust quickly gets up to strength
                        //Note that this will also help the nanocaust get stronger again if it has lost a bunch of planets
                        if ( baseTimeBetweenFrenzies + mgr.timeModifierForNextFrenzy < minTimeBetweenFrenzies )
                            mgr.timeModifierForNextFrenzy = minTimeBetweenFrenzies - baseTimeBetweenFrenzies;
                        if ( mgr.nanobotCenterPlanets.Count < planetsForEarlyExpansion )
                            mgr.timeModifierForNextFrenzy = minTimeBetweenFrenzies - baseTimeBetweenFrenzies;

                    }
                    if ( deleteFleet )
                    {
                        //remove the fleetID from any remaining ships
                        List<GameEntity> shipsForFleet = fleet.ships;
                        for ( int j = 0; j < shipsForFleet.Count; j++ )
                        {
                            shipsForFleet[j].SetNanocaustFleetId( -1 );
                        }
                        mgr.fleets.Remove( fleet );
                    }
                }

                //check if we should frenzy
                if ( World_AIW2.Instance.GameSecond >= mgr.timeForNextFrenzy )
                {
                    FrenzyGoal goal;
                    //This if/else section determines what we are frenzying agains
                    //The first two conditionals in this if/else section are overrides
                    //Option 1 is for the beginning of the game
                    //Option 2 is for once the Nanocaust has been hacked
                    if ( mgr.nanobotCenterPlanets.Count < planetsForEarlyExpansion )
                    {
                        mgr.modifierMaxStrengthPerPlanetForFrenzy += 50000; //make early waves extra strong
                        goal = FrenzyGoal.ExpandWeakest;
                    }
                    else if ( mgr.hasBeenHacked )
                    {
                        //The Nanocaust is now helping the humans
                        int rand = Context.QualityRandom.Next( 0, 30 );
                        if ( rand < 15 )
                            goal = FrenzyGoal.ExpandTowardAI;
                        else
                            goal = FrenzyGoal.AIKing;
                    }
                    else
                    {
                        //this is a pretty crude method of choosing which type of
                        //wave to do, but it suffices for the moment
                        int rand = Context.QualityRandom.Next( 0, 100 );
                        if ( rand < 50 )
                        {
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( " Expand outwardFromHive", Verbosity.DoNotShow );
                            goal = FrenzyGoal.ExpandOutwardFromHive;
                        }
                        else if ( rand < 60 )
                        {
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( " Expand weakest", Verbosity.DoNotShow );
                            goal = FrenzyGoal.ExpandWeakest;

                        }
                        else if ( rand < 70 )
                        {
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( " Expand randomly", Verbosity.DoNotShow );
                            goal = FrenzyGoal.ExpandRandom;
                        }
                        else if ( rand < 80 )
                        {
                            goal = FrenzyGoal.ExpandTowardHuman;
                        }
                        else if ( rand < 90 )
                        {
                            goal = FrenzyGoal.ExpandTowardAI;
                        }
                        else if ( mgr.humanVision )
                        {
                            //remaining percentage goes for one of the kings
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( " human king wave", Verbosity.DoNotShow );

                            goal = FrenzyGoal.HumanKing;
                        }
                        else
                        {
                            if ( localDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( " ai king wave", Verbosity.DoNotShow );
                            goal = FrenzyGoal.AIKing;
                        }
                    }

                    Planet frenzyTarget = getFrenzyTarget( side, goal, Context );
                    Planet stagingArea = getStagingArea( frenzyTarget, Context, side );
                    FrenzyFleet fleet = new FrenzyFleet( frenzyTarget, stagingArea, goal );
                    //Note that I think I can mix maxStrength and maxStrengthPerPlanet (ie 500 per planet, up to a max of 5000)
                    //haven't tested this but it might be useful
                    fleet.maxStrengthPerPlanetForFleet = baseMaxStrengthPerPlanetForFrenzy + mgr.modifierMaxStrengthPerPlanetForFrenzy;
                    fleet.time = World_AIW2.Instance.GameSecond;
                    fleet.fleetId = mgr.numFleetsSent;
                    mgr.numFleetsSent++;
                    mgr.fleets.Add( fleet );

                    mgr.timeForNextFrenzy = World_AIW2.Instance.GameSecond + baseTimeBetweenFrenzies + mgr.timeModifierForNextFrenzy;
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( " FrenzyFleet " + fleet, Verbosity.DoNotShow );
                }

                addConstructorsToConqueredPlanets( side, Context );

                upgradeConstructors( side, Context );

                adjustAIProgress( Context );

                //adjust nanobot lifespans when appropriate
                int numPlanetsOverMin = Math.Max( mgr.nanobotCenterPlanets.Count - planetsForEarlyExpansion, 0 );
                int lifespanFactor = 40; //chosen capriciously
                int maxNanobotLifespan = nanocaustConstants.GetInt( "maxNanobotLifespan" );
                int minNanobotLifespan = nanocaustConstants.GetInt( "minNanobotLifespan" );
                mgr.nanobotLifespan = maxNanobotLifespan - lifespanFactor * numPlanetsOverMin;
                if ( mgr.nanobotLifespan < minNanobotLifespan )
                    mgr.nanobotLifespan = minNanobotLifespan;

                checkForHumanVision();
                mgr.hasSimStepRun = true; //LongRangePlanning needs to make sure PerSimStep has been run


                //TODO: I'd love to also be able to check "are the humans close" and "Have the humans ever destroyed
                //a nanobot center" for some additional anti-human behaviour...
            }
            World.Instance.SetNanocaustMgr( mgr );
        }
        public void adjustAIProgress( ArcenSimContext Context )
        {
            bool localDebug = false;
            //adjusts the AI Progress appropriately when a planet is captured by the nanocaust
            //mostly a hack (for example, if a human conquers a nanocaust planet then it won't
            //correctly reset the progress reduction to account for that so it will be free for the human
            int requiredProgressModification = ( mgr.totalPlanetsEverTaken.Count * 20 );
            int progressDifference = requiredProgressModification - SpecialFaction_Nanocaust.Instance.mgr.progressModification;

            if ( progressDifference != 0 )
            {
                //we may need to adjust the progress. Note: Foreach planet with a nanobaust center, we should check if it has a planetary constructor
                //or a warp gate in case we only destroyed one. I haven't bothered since this is mostly a hack

                FInt modifier = FInt.FromParts( progressDifference, 000 );
                modifier *= -1;
                int previousEffectiveProgress = World_AIW2.Instance.AIProgress_Effective.IntValue;
                int previousTotalProgress = World_AIW2.Instance.AIProgress_Total.IntValue;
                int previousReductionProgress = World_AIW2.Instance.AIProgress_Reduction.IntValue;
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "There are " + SpecialFaction_Nanocaust.Instance.mgr.nanobotCenterPlanets.Count + " Nanocaust centers. Prev adjustment " + SpecialFaction_Nanocaust.Instance.mgr.progressModification + " difference " + progressDifference + " Adjusting AIP by " + modifier, Verbosity.DoNotShow );

                World_AIW2.Instance.ChangeAIP( modifier, AIPChangeReason.EntityDeath, null, Context );
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Previous effective: " + previousEffectiveProgress + " total " + previousTotalProgress + " reduction " + previousReductionProgress + " new effective: " + World_AIW2.Instance.AIProgress_Effective.IntValue + " new total " + World_AIW2.Instance.AIProgress_Total.IntValue + " new reduction " + World_AIW2.Instance.AIProgress_Reduction.IntValue, Verbosity.DoNotShow );

                SpecialFaction_Nanocaust.Instance.mgr.progressModification = requiredProgressModification;
            }
        }
        public void doInitialPlanetStuff( WorldSide side, ArcenSimContext Context )
        {
            //For the first planet, give us a few extra ships and destroy all the AI Guardians,
            //just so we get off on the right foot
            GameEntityTypeData aberrationData = GameEntityTypeDataTable.Instance.GetRowByName( "Aberration", true, null );
            GameEntityTypeData abominationData = GameEntityTypeDataTable.Instance.GetRowByName( "Abomination", true, null );
            int numEntityToSpawn = 3;

            for ( int i = 0; i < mgr.nanobotCenterPlanets.Count; i++ )
            {
                //if we are seeding multiple starting nanobot construction centers
                CombatSide cside = mgr.nanobotCenterPlanets[i].Combat.GetSideForWorldSide( side );
                ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                for ( int j = 0; j < numEntityToSpawn; j++ )
                {
                    GameEntity.CreateNew( cside, aberrationData, center, Context );
                    GameEntity.CreateNew( cside, abominationData, center, Context );
                }
                //kill the guardians on the planet (this is needed to make sure we don't die to a Mark IV planet on spawn)
                cside.DoForRelatedSides( SideRelationship.SidesThatAreHostileTowardsMe, delegate ( CombatSide otherSide )
                                         {
                                             otherSide.Entities.DoForEntities( EntityRollupType.BlocksEnemyClaimFlows, delegate ( GameEntity entity )
                                                                           {
                                                                                 entity.Die( Context );
                                                                                 return DelReturn.Continue;
                                                                             } );
                                             return DelReturn.Continue;
                                         } );
            }
        }

        public void addConstructorsToConqueredPlanets( WorldSide side, ArcenSimContext Context )
        {

            bool localDebug = false;
            if ( mgr.fleets.Count == 0 ) //no active fleets
                return;
            List<Planet> planetsToCapture = null;
            //process the fleets to see if we are allowed to capture anything
            for ( int i = 0; i < mgr.fleets.Count; i++ )
            {
                bool canExpand = false;
                //the following are the frenzy goals that allow capture
                if ( mgr.fleets[i].goal == FrenzyGoal.ExpandRandom || mgr.fleets[i].goal == FrenzyGoal.ExpandWeakest ||
                   mgr.fleets[i].goal == FrenzyGoal.ExpandOutwardFromHive ||
                   mgr.fleets[i].goal == FrenzyGoal.ExpandTowardHuman || mgr.fleets[i].goal == FrenzyGoal.ExpandTowardAI )
                    canExpand = true;
                if ( NanocaustUtilityMethods.isPlanetOnList( mgr.nanobotCenterPlanets, mgr.fleets[i].target ) )
                    continue; //don't place a constructor if we have one already

                if ( canExpand && allowedToCapture( side, mgr.fleets[i].target ) )
                {
                    if ( planetsToCapture == null )
                        planetsToCapture = new List<Planet>();
                    if ( !NanocaustUtilityMethods.isPlanetOnList( planetsToCapture, mgr.fleets[i].target ) )
                    {
                        //two fleets could be attacking the same target
                        planetsToCapture.Add( mgr.fleets[i].target );

                    }
                }
            }
            if ( planetsToCapture != null )
            {
                for ( int i = 0; i < planetsToCapture.Count; i++ )
                {

                    if ( !NanocaustUtilityMethods.isPlanetOnList( mgr.totalPlanetsEverTaken, planetsToCapture[i] ) )
                        mgr.totalPlanetsEverTaken.Add( mgr.fleets[i].target ); //update the list of "all planets we've ever captured"
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( " Adding constructor to " + planetsToCapture[i].Name, Verbosity.DoNotShow );
                    //add a nanobot constructor
                    CombatSide cside = planetsToCapture[i].Combat.GetSideForWorldSide( side );
                    GameEntityTypeData nanobotData = GameEntityTypeDataTable.Instance.GetRowByName( "NanobotCenter_Mark1", true, null );
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    GameEntity newConstructor = GameEntity.CreateNew( cside, nanobotData, center, Context );
                    //                  newConstructor.SelfBuildingMetalRemaining = (FInt)nanobotData.BalanceStats.SquadMetalCost; //self building doesn't seem to work right
                }
            }
        }

        //public void addConstructorIfPossible( WorldSide side, ArcenSimContext Context )
        //{
        //    //This was the "Mark 2" version that just checked arbitrarily if we were stronger on any given planet;
        //    //What I want instead is to only conquer plantes we've chosen to deliberately, so this has been deprecated
        //    //but it is currently left for reference


        //    //See addConstructorsToConqueredPlanets
        //    //checks all planets with nanobot presence to see if we have "conquered" the planet;
        //    //conquering is based on relative nanobot to enemy strength.
        //    //attempt to place constructor on mgr.frenzyTarget
        //    bool localDebug = false;
        //    if ( localDebug )
        //        ArcenDebugging.ArcenDebugLogSingleLine( " Testing all planets to allow constructors to be placed", Verbosity.DoNotShow );

        //    //Read some constants to let us figure out whether we are allowed to capture a planet
        //    CustomDataSet nanocaustConstants = ExternalConstants.Instance.GetCustomData( "nanocaust" );
        //    int HumanStrengthForCapture = nanocaustConstants.GetInt( "humanStrengthForCapture" );
        //    int AIStrengthForCapture = nanocaustConstants.GetInt( "AIStrengthForCapture" );
        //    int captureStrengthRequired = nanocaustConstants.GetInt( "captureStrengthRequired" );
        //    ArcenSparseLookup<Planet, int> strengthPerPlanet = factionStrengthPerPlanet( side );
        //    for ( int i = 0; i < strengthPerPlanet.GetPairCount(); i++ )
        //    {
        //        ArcenSparseLookupPair<Planet, int> pair = strengthPerPlanet.GetPairByIndex( i );
        //        int humanStrengthForPlanet = (int)pair.Key.HumanTotalStrength;
        //        int AIStrengthForPlanet = (int)pair.Key.AITotalStrength;
        //        int factionStrength = 0;
        //        if ( NanocaustUtilityMethods.isPlanetOnList( mgr.nanobotCenterPlanets, pair.Key ) )
        //            continue; //don't place multiple constructors
        //        factionStrength = pair.Value;
        //        if ( humanStrengthForPlanet <= HumanStrengthForCapture && AIStrengthForPlanet <= AIStrengthForCapture && factionStrength > captureStrengthRequired )
        //        {
        //            CombatSide cside = pair.Key.Combat.GetSideForWorldSide( side );
        //            GameEntityTypeData centerData = GameEntityTypeDataTable.Instance.GetRowByName( "NanobotCenter_Mark1", true, null );
        //            ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
        //            GameEntity.CreateNew( cside, centerData, center, Context );
        //        }
        //    }
        //}

        //checks whether the humans have seen the Nanocaust
        //I hope this will eventually give a nice gameplay popup saying
        //"Uh Commander, we've spotted something scary....."
        //also this will allow the Nanocaust to change it's behavour to make it more human focused
        public void checkForHumanVision()
        {
            if ( mgr.humanVision == false )
            {
                for ( int i = 0; i < mgr.nanobotCenterPlanets.Count; i++ )
                {
                    Planet planet = mgr.nanobotCenterPlanets[i];
                    if ( planet.HumansHaveBasicIntel )
                    {
                        mgr.humanVision = true;
                    }
                }
            }
        }

        public bool allowedToCapture( WorldSide side, Planet planet )
        {
            bool debug = false;
            //helper function for figuring out if we can capture a planet
            //currently it is intended to say "Have all the defenses been destroyed"
            //unsure if it works properly for human planets, but I haven't been able to get a game
            //far enough in to test it....
            FInt humanStrength = FInt.Zero;
            FInt aiStrength = FInt.Zero;
            for ( int i = 0; i < planet.Combat.Sides.Count; i++ )
            {
                CombatSide otherCombatSide = planet.Combat.Sides[i];
                FInt sideStrength = otherCombatSide.DataByStance[SideStance.Self].TotalStrength;
                switch ( otherCombatSide.WorldSide.Type )
                {
                    case WorldSideType.AI:
                        aiStrength += sideStrength;
                        break;
                    case WorldSideType.Player:
                        humanStrength += sideStrength;
                        break;
                }
            }
            CombatSide myCombatSide = planet.Combat.GetSideForWorldSide( side );
            FInt myStrength = myCombatSide.DataByStance[SideStance.Self].TotalStrength;
            CustomDataSet nanocaustConstants = ExternalConstants.Instance.GetCustomData( "nanocaust" );
            int humanStrengthForCapture = nanocaustConstants.GetInt( "humanStrengthForCapture" );
            int AIStrengthForCapture = nanocaustConstants.GetInt( "AIStrengthForCapture" );
            int captureStrengthRequired = nanocaustConstants.GetInt( "captureStrengthRequired" );
            if ( humanStrength <= humanStrengthForCapture && aiStrength <= AIStrengthForCapture && myStrength > captureStrengthRequired )
            {
                return true;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "allowedToCapture: currently can't capture " + planet.Name + " Nanocaust Strength " + myStrength + " AI strength " + aiStrength + " human strength " + humanStrength, Verbosity.DoNotShow );
            return false;
        }

        //updates the mgr class planet lists. Must be called after the
        //nanobot center list is updated
        public void updatePlanetLists()
        {
            mgr.activePlanets.Clear();
            mgr.quiescedPlanets.Clear();
            mgr.uninfectedPlanets.Clear();
            if ( mgr.nanobotCenterPlanets.Count == 0 )
                return;
            //Now that I've found all the planets with constructors, see which of them have neighbors not on the list
            //for potential targets...
            for ( int i = 0; i < mgr.nanobotCenterPlanets.Count; i++ )
            {
                Planet planet = mgr.nanobotCenterPlanets[i];
                bool neighborNotOnList = false;
                planet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                {
                    bool neighborOnList = false;
                    for ( int j = 0; j < mgr.nanobotCenterPlanets.Count; j++ )
                    {
                        if ( neighbor == mgr.nanobotCenterPlanets[i] )
                            continue;
                        if ( neighbor == mgr.nanobotCenterPlanets[j] )
                            neighborOnList = true;
                    }
                    if ( !neighborOnList )
                    {
                        mgr.uninfectedPlanets.Add( neighbor );
                        neighborNotOnList = true;
                    }
                    return DelReturn.Continue;
                } );
                if ( neighborNotOnList )
                    mgr.activePlanets.Add( planet );
                else
                    mgr.quiescedPlanets.Add( planet );
            }
        }

        public void upgradeConstructors( WorldSide side, ArcenSimContext Context )
        {
            //If a Constructor has lived long enough, upgrade it to the next version
            bool localDebug = false;
            CustomDataSet nanocaustConstants = ExternalConstants.Instance.GetCustomData( "nanocaust" );
            int timeForMarkIIUpgrade = nanocaustConstants.GetInt( "timeForMarkIIUpgrade" );
            int timeForMarkIIIUpgrade = nanocaustConstants.GetInt( "timeForMarkIIIUpgrade" );
            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( localDebug && entity.TypeData.Name.Contains( "Nanobot Center" ) )
                    ArcenDebugging.ArcenDebugLogSingleLine( "upgradeConstructors: scanning entity whose name is  " + entity.TypeData.Name + " alive for " + entity.SecondsSinceEnteringThisPlanet, Verbosity.DoNotShow );
                if ( entity.TypeData.Tags.Contains( "NanobotCenter1" ) && entity.SecondsSinceCreation > timeForMarkIIUpgrade )
                {
                    Planet planet = entity.Combat.Planet; //find the planet for a specific entity
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "UpgradeConstructors: found nanobot center Mark 1 on  " + planet.Name + " alive " + entity.SecondsSinceEnteringThisPlanet + " seconds", Verbosity.DoNotShow );
                    GameEntityTypeData centerData = GameEntityTypeDataTable.Instance.GetRowByName( "NanobotCenter_Mark2", true, null );
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    CombatSide cside = planet.Combat.GetSideForWorldSide( side );
                    entity.Die( Context );
                    GameEntity.CreateNew( cside, centerData, center, Context );
                }
                if ( entity.TypeData.Tags.Contains( "NanobotCenter2" ) && entity.SecondsSinceCreation > timeForMarkIIIUpgrade )
                {
                    Planet planet = entity.Combat.Planet;
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "UpgradeConstructors: found nanobot center Mark II on  " + planet.Name + " alive " + entity.SecondsSinceEnteringThisPlanet + " seconds", Verbosity.DoNotShow );
                    GameEntityTypeData centerData = GameEntityTypeDataTable.Instance.GetRowByName( "NanobotCenter_Mark3", true, null );
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    CombatSide cside = planet.Combat.GetSideForWorldSide( side );
                    entity.Die( Context );
                    GameEntity.CreateNew( cside, centerData, center, Context );
                }

                return DelReturn.Continue;
            } );
        }

        //These are planets that currently have nanobot constructors
        public void findPlanetsWithConstructors( WorldSide side, List<Planet> listToFill, bool clearFirst )
        {
            bool localDebug = false; //use a local variable because this logging is very noisy
                                     //and the code mostly works
            if ( clearFirst ) listToFill.Clear();
            // mgr.hivePlanet = null;
            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "GetPlanetsWithConstructors: scanning entity whose name is  " + entity.TypeData.Name, Verbosity.DoNotShow );
                if ( ArcenStrings.ListContains( entity.TypeData.Tags, "NanobotCenter" ) )
                {
                    Planet planet = entity.Combat.Planet;
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "getPlanetsWithConstructors: found nanobot center on  " + planet.Name, Verbosity.DoNotShow );
                    listToFill.Add( planet );
                    if ( ArcenStrings.ListContains( entity.TypeData.Tags, NANOCAUST_HIVE ) )
                    {
                        mgr.hivePlanet = planet;
                        if ( !NanocaustUtilityMethods.isPlanetOnList( mgr.totalPlanetsEverTaken, planet ) )
                            mgr.totalPlanetsEverTaken.Add( planet );
                    }
                    if ( !mgr.hasBeenHacked )
                    {
                        //If we have reloaded a game where the Hive was hacked, correct our state
                        if ( ArcenStrings.ListContains( entity.TypeData.Tags, NANOCAUST_HACKED_HIVE ) )
                        {
                            mgr.hasBeenHacked = true;
                        }
                    }
                }
                return DelReturn.Continue;
            } );
        }
        //returns a dictionary that maps from Planet to factionStrength on that planet
        public ArcenSparseLookup<Planet, int> factionStrengthPerPlanet( WorldSide side )
        {
            ArcenSparseLookup<Planet, int> strengthPerPlanet = new ArcenSparseLookup<Planet, int>();
            side.Entities.DoForEntities( GameEntityCategory.Ship, delegate ( GameEntity entity )
            {
                if ( entity.LongRangePlanningData == null )
                    return DelReturn.Continue; // if created after the start of this planning cycle, skip
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.LongRangePlanningData.CurrentPlanetIndex );
                int entityStrength = (int)NanocaustUtilityMethods.strengthOfEntity( entity );
                if ( !strengthPerPlanet.GetHasKey( planet ) )
                {
                    strengthPerPlanet[planet] = entityStrength;
                }
                else
                    strengthPerPlanet[planet] += entityStrength;
                return DelReturn.Continue;
            } );

            return strengthPerPlanet;
        }
        public Planet getFrenzyTarget( WorldSide side, FrenzyGoal goal, ArcenSimContext Context )
        {
            //we might want to make this a static in UtilityMethods to avoid the List
            bool localDebug = false;

            //handle anti human/AI king waves seperately
            if ( goal == FrenzyGoal.AIKing )
            {
                Planet AIKingPlanet = NanocaustUtilityMethods.findAIKing();
                if ( AIKingPlanet == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "The AI King seems to be dead? switch to expandRandom", Verbosity.DoNotShow );
                    goal = FrenzyGoal.ExpandRandom;
                }
                else
                    return AIKingPlanet;
            }
            if ( goal == FrenzyGoal.HumanKing )
            {
                Planet HumanKingPlanet = NanocaustUtilityMethods.findHumanKing();
                if ( HumanKingPlanet == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "The Human King seems to be dead? switch to expandRandom", Verbosity.DoNotShow );
                    goal = FrenzyGoal.ExpandRandom;
                }
                else
                    return HumanKingPlanet;
            }
            if ( goal == FrenzyGoal.ExpandWeakest )
            {
                //conquer the weakest nearby planet
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Choosing the weakest planet to take", Verbosity.DoNotShow );
                int targetIdx = -1;
                FInt strSoFar = FInt.FromParts( -1, 000 );
                for ( int i = 0; i < mgr.uninfectedPlanets.Count; i++ )
                {
                    FInt enemyStrength = mgr.uninfectedPlanets[i].Combat.GetSideForWorldSide( side ).DataByStance[SideStance.Hostile].TotalStrength;
                    if ( ( strSoFar > enemyStrength ) || strSoFar == -1 )
                    {
                        targetIdx = i;
                        strSoFar = enemyStrength;
                    }
                }
                return mgr.uninfectedPlanets[targetIdx];
            }
            else if ( goal == FrenzyGoal.ExpandOutwardFromHive )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Choosing the planet closest to the hive", Verbosity.DoNotShow );
                List<Planet> planetsToCheck = new List<Planet>();
                //simple case;
                planetsToCheck.Add( mgr.hivePlanet );
                for ( int i = 0; i < planetsToCheck.Count; i++ )
                {
                    Planet current = planetsToCheck[i];
                    Planet target = null;
                    current.DoForLinkedNeighbors( delegate ( Planet neighbor )
                                                  {
                                                      if ( NanocaustUtilityMethods.isPlanetOnList( mgr.uninfectedPlanets, neighbor ) )
                                                          target = neighbor;
                                                      if ( !planetsToCheck.Contains( neighbor ) )
                                                          planetsToCheck.Add( neighbor );
                                                      return DelReturn.Continue;
                                                  } );
                    if ( target != null )
                        return target;
                }
                //We've now looked at all the planets and they all seem to have nanobot centers...
                ArcenDebugging.ArcenDebugLogSingleLine( "The nanocaust seems to have conquered the galaxy....", Verbosity.DoNotShow );
                return null;
            }
            else if ( goal == FrenzyGoal.ExpandTowardAI )
            {
                List<Planet> pathToKing = new List<Planet>();
                pathToKing.Clear();
                //if the humans have hacked us, go for the AI king
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Expanding toward the AI", Verbosity.DoNotShow );
                Planet kingPlanet = NanocaustUtilityMethods.findAIKing();
                pathToKing = side.Pathfinder.FindPath( mgr.hivePlanet, kingPlanet, 0, 0 );
                for ( int i = 0; i < pathToKing.Count; i++ )
                {
                    if ( !NanocaustUtilityMethods.isPlanetOnList( mgr.nanobotCenterPlanets, pathToKing[i] ) )
                    {
                        return pathToKing[i];
                    }
                }
            }
            else if ( goal == FrenzyGoal.ExpandTowardHuman )
            {
                List<Planet> pathToKing = new List<Planet>();
                pathToKing.Clear();

                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Expanding toward the humans", Verbosity.DoNotShow );
                Planet kingPlanet = NanocaustUtilityMethods.findHumanKing();
                pathToKing = side.Pathfinder.FindPath( mgr.hivePlanet, kingPlanet, 0, 0 );
                for ( int i = 0; i < pathToKing.Count; i++ )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + i + " is " + pathToKing[i].Name, Verbosity.DoNotShow );
                    if ( !NanocaustUtilityMethods.isPlanetOnList( mgr.nanobotCenterPlanets, pathToKing[i] ) )
                    {
                        if ( localDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "This planet has no nanobot center. take it next", Verbosity.DoNotShow );
                        return pathToKing[i];
                    }
                }
            }
            else if ( goal == FrenzyGoal.ExpandRandom )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "choosing planet at random", Verbosity.DoNotShow );

                return mgr.uninfectedPlanets[Context.QualityRandom.Next( 0, mgr.uninfectedPlanets.Count )]; //pick at randmo
            }

            //Default case; just pretend this is "ExpandRandom"
            ArcenDebugging.ArcenDebugLogSingleLine( "BUG! unknown frenzyGoal", Verbosity.DoNotShow );
            return mgr.uninfectedPlanets[Context.QualityRandom.Next( 0, mgr.uninfectedPlanets.Count )]; //pick at randmo

        }
        Planet getStagingArea( Planet frenzyTarget, ArcenSimContext Context, WorldSide side )
        {
            //the staging area is the Nanocaust planet that the fleet meets at before setting out
            //Note it is currently possible to have this code send the nanocaust ships through non-nanocaust
            //planets on the way to the staging area. 
            bool localDebug = false;
            Planet stageArea = null;
            frenzyTarget.DoForLinkedNeighbors( delegate ( Planet neighbor )
                                               {
                                                   if ( NanocaustUtilityMethods.isPlanetOnList( mgr.nanobotCenterPlanets, neighbor ) )
                                                       stageArea = neighbor;
                                                   return DelReturn.Continue;
                                               } );

            if ( stageArea != null )
            {
                //if we have a nanocaust planet right next to our target, just stage there
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Found adjacent planet for staging: " + stageArea.Name, Verbosity.DoNotShow );

                return stageArea;
            }

            //if the target is no immediately adjoining a nanobot planet, pick the planet
            //closest to the target on a line from the nanobot hive
            //sometimes this can result in unexpected paths if there are multiple equivalent
            //paths to the AI
            List<Planet> pathToFrenzy = new List<Planet>();
            pathToFrenzy = side.Pathfinder.FindPath( mgr.hivePlanet, frenzyTarget, 0, 0 );
            //make a path between the nanobot Hive and the target
            for ( int i = 0; i < pathToFrenzy.Count; i++ )
            {
                if ( NanocaustUtilityMethods.isPlanetOnList( mgr.nanobotCenterPlanets, pathToFrenzy[i] ) )
                    stageArea = pathToFrenzy[i]; //if this is a nanobot planet, update the stage area
                else
                    break; //no longer nanobot planet; end of the loop
            }
            if ( localDebug )
            {
                if ( stageArea == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "staging area is null" + stageArea.Name, Verbosity.DoNotShow );
                else
                    ArcenDebugging.ArcenDebugLogSingleLine( "staging area " + stageArea.Name, Verbosity.DoNotShow );
            }
            return stageArea;
        }
    }
    /* End Nanocaust Special Faction */

    //These are the zombies created by the zombifying guns of the nanocaust
    public class SpecialFaction_NanobotZombie : ISpecialFactionImplementation
    {
        public static SpecialFaction_NanobotZombie Instance;
        public SpecialFaction_NanobotZombie() { Instance = this; }

        public void SetStartingSideRelationships( WorldSide side )
        {
            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide otherSide = World_AIW2.Instance.Sides[i];
                if ( side == otherSide )
                    continue;
                switch ( otherSide.Type )
                {
                    case WorldSideType.Player:
                        if ( SpecialFaction_Nanocaust.Instance.mgr.hasBeenHacked )
                        {
                            side.MakeFriendlyTo( otherSide );
                            otherSide.MakeFriendlyTo( side );
                            break;
                        }
                        //note that the "else" for this if is a fallthrough to the next case
                        break;
                    case WorldSideType.AI:
                        side.MakeHostileTo( otherSide );
                        otherSide.MakeHostileTo( side );
                        break;

                    case WorldSideType.SpecialFaction:
                        SpecialFactionData data = otherSide.SpecialFactionData;
                        if ( data.Name != "Nanocaust" ) //TODO: remove this use of Name
                        {
                            //if you aren't the Nanocaust, we hate you
                            side.MakeHostileTo( otherSide );
                            otherSide.MakeHostileTo( side );
                            break;
                        }
                        side.MakeFriendlyTo( otherSide );
                        otherSide.MakeFriendlyTo( side );
                        break;
                    default:
                        break;
                }
            }
        }

        public ArcenEnumIndexedArray_AIBudgetType<FInt> GetSpendingRatios( WorldSide side )
        {
            ArcenEnumIndexedArray_AIBudgetType<FInt> result = new ArcenEnumIndexedArray_AIBudgetType<FInt>();

            result[AIBudgetType.Reinforcement] = FInt.One;

            return result;
        }

        public bool GetShouldAttackNormallyExcludedTarget( WorldSide side, GameEntity Target )
        {
            return false;
        }

        public void SeedStartingEntities( WorldSide side, Galaxy galaxy, ArcenSimContext Context, MapTypeData mapType )
        {
        }

        public void DoLongRangePlanning( WorldSide side, ArcenLongTermPlanningContext Context )
        {
        }

        public void DoPerSimStepLogic( WorldSide side, ArcenSimContext Context )
        {
        }
    }

    /* Helper functions for the nanocaust. I'm sure there are more efficient ways
       to do many of these things... */
    public static class NanocaustUtilityMethods
    {
        public static FInt strengthOfEntity( GameEntity entity )
        {
            return entity.TypeData.BalanceStats.StrengthPerSquad + entity.LongRangePlanningData.StrengthOfContents;
        }
        public static FInt strengthOfList( List<GameEntity> list )
        {

            FInt str = FInt.FromParts( 0, 0 );
            if ( list == null )
                return str;
            for ( int i = 0; i < list.Count; i++ )
            {
                str += strengthOfEntity( list[i] );
            }
            return str;
        }

        /* I feel really dumb for writing this function, since I'm 100% sure there's
           a really easy method for doing this... */
        public static bool isPlanetOnList( List<Planet> list, Planet element )
        {
            bool localDebug = false;
            if ( list == null || element == null )
                return false;

            for ( int i = 0; i < list.Count; i++ )
            {
                if ( element == list[i] )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( " isPlanetOnList " + i + " " + element.Name + " == " + list[i].Name, Verbosity.DoNotShow );
                    return true;
                }
                else
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( " isPlanetOnList " + i + " " + element.Name + " != " + list[i].Name, Verbosity.DoNotShow );
                }
            }
            return false;
        }
        public static void makeFriendsWithHumans( WorldSide side )
        {

            for ( int i = 0; i < World_AIW2.Instance.Sides.Count; i++ )
            {
                WorldSide otherSide = World_AIW2.Instance.Sides[i];
                if ( side == otherSide )
                    continue;
                switch ( otherSide.Type )
                {
                    case WorldSideType.NaturalObject:
                        side.MakeFriendlyTo( otherSide );
                        otherSide.MakeFriendlyTo( side );
                        break;

                    case WorldSideType.Player:
                        if ( SpecialFaction_Nanocaust.Instance.mgr.hasBeenHacked )
                        {
                            side.MakeFriendlyTo( otherSide );
                            otherSide.MakeFriendlyTo( side );
                        }
                        break;

                    case WorldSideType.SpecialFaction:
                        SpecialFactionData data = otherSide.SpecialFactionData;
                        if ( data.Name == "Nanocaust" ) //TODO: remove this use of Name
                        {
                            //if you aren't the Nanocaust, we hate you
                            side.MakeFriendlyTo( otherSide );
                            otherSide.MakeFriendlyTo( side );
                        }
                        else
                        {
                            side.MakeFriendlyTo( otherSide );
                            otherSide.MakeFriendlyTo( side );

                        }
                        break;
                    default:
                        side.MakeHostileTo( otherSide );
                        otherSide.MakeHostileTo( side );
                        break;
                }


            }
        }
        public static Planet findHumanKing()
        {
            bool debug = false;
            Planet kingPlanet = null;

            World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnits, delegate ( GameEntity entity )
                                                  {
                                                      if ( entity.Side.WorldSide.Type == WorldSideType.Player )
                                                          kingPlanet = entity.Combat.Planet;
                                                      return DelReturn.Continue;
                                                  } );
            if ( kingPlanet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No human king found?!?!?!?", Verbosity.DoNotShow );
            }
            else if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "human king on " + kingPlanet.Name, Verbosity.DoNotShow );
            return kingPlanet;
        }
        public static Planet findAIKing()
        {
            bool debug = false;
            Planet kingPlanet = null;

            World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnits, delegate ( GameEntity entity )
                                                  {
                                                      if ( entity.Side.WorldSide.Type != WorldSideType.Player )
                                                          kingPlanet = entity.Combat.Planet;
                                                      return DelReturn.Continue;
                                                  } );
            if ( kingPlanet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No AI king found?!?!?!?", Verbosity.DoNotShow );
            }
            else if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "AI king on " + kingPlanet.Name, Verbosity.DoNotShow );
            return kingPlanet;
        }

        public static void Helper_RaidAgainstKing( List<GameEntity> kingFleet, Planet originPlanet, WorldSide side, ArcenLongTermPlanningContext Context, bool antiHuman )
        {
            bool debug = true;
            Planet kingPlanet = null;
            //Find the kingPlanet
            if ( antiHuman )
            {

                World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnits, delegate ( GameEntity entity )
                                                  {
                                                      if ( entity.Side.WorldSide.Type == WorldSideType.Player )
                                                          kingPlanet = entity.Combat.Planet;
                                                      return DelReturn.Continue;
                                                  } );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Killing the human king on" + kingPlanet.Name, Verbosity.DoNotShow );

            }
            else
            {
                World_AIW2.Instance.DoForEntities( EntityRollupType.KingUnits, delegate ( GameEntity entity )
                                                  {
                                                      if ( entity.Side.WorldSide.Type != WorldSideType.Player )
                                                          kingPlanet = entity.Combat.Planet;
                                                      return DelReturn.Continue;
                                                  } );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Killing the AI king on" + kingPlanet.Name, Verbosity.DoNotShow );

            }
            List<Planet> pathToTarget = new List<Planet>();
            pathToTarget = side.Pathfinder.FindPath( originPlanet, kingPlanet, 0, 0 );

            if ( pathToTarget.Count <= 0 )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Seems the king is already dead?", Verbosity.DoNotShow );
                return;
            }

            GameCommand command = GameCommand.Create( GameCommandType.SetWormholePath );
            for ( int k = 0; k < kingFleet.Count; k++ )
                command.RelatedEntityIDs.Add( kingFleet[k].PrimaryKeyID );
            for ( int k = 0; k < pathToTarget.Count; k++ )
                command.RelatedPlanetIndices.Add( pathToTarget[k].PlanetIndex );
            Context.QueueCommandForSendingAtEndOfContext( command );
        }

        //Sends a raid against a single specific planet
        //Needs a flag for "Only stage through nanocaut planets if possible" flag
        public static void Helper_RaidSpecificPlanet( List<GameEntity> ships, Planet originPlanet, WorldSide worldSide, Galaxy galaxy, Planet threatplanet, bool IgnorePathCosts, ArcenLongTermPlanningContext Context )
        {

            /* Set up the data to let us get from any planet to any other given planet */
            for ( int k = 0; k < galaxy.Planets.Count; k++ )
            {
                Planet otherPlanet = galaxy.Planets[k];
                otherPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom = null;
                otherPlanet.FactionPlanning_CheapestRaidPathToHereCost = FInt.Zero;
            }
            List<Planet> potentialAttackTargets = new List<Planet>();
            List<Planet> planetsToCheckInFlood = new List<Planet>();
            planetsToCheckInFlood.Add( originPlanet );
            originPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom = originPlanet;
            for ( int k = 0; k < planetsToCheckInFlood.Count; k++ )
            {
                Planet floodPlanet = planetsToCheckInFlood[k];
                floodPlanet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                {
                    FInt totalCostFromOriginToNeighbor = floodPlanet.FactionPlanning_CheapestRaidPathToHereCost + 1;
                    if ( !potentialAttackTargets.Contains( neighbor ) )
                        potentialAttackTargets.Add( neighbor );
                    if ( neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom != null &&
                         neighbor.FactionPlanning_CheapestRaidPathToHereCost <= totalCostFromOriginToNeighbor )
                        return DelReturn.Continue;
                    neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom = floodPlanet;
                    neighbor.FactionPlanning_CheapestRaidPathToHereCost = totalCostFromOriginToNeighbor;
                    planetsToCheckInFlood.Add( neighbor );
                    return DelReturn.Continue;
                } );
            }


            List<Planet> path = new List<Planet>();
            Planet workingPlanet = threatplanet;
            while ( workingPlanet != originPlanet )
            {
                path.Insert( 0, workingPlanet );
                workingPlanet = workingPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom;
            }
            if ( path.Count > 0 )
            {
                GameCommand command = GameCommand.Create( GameCommandType.SetWormholePath );
                for ( int k = 0; k < ships.Count; k++ )
                    command.RelatedEntityIDs.Add( ships[k].PrimaryKeyID );
                for ( int k = 0; k < path.Count; k++ )
                    command.RelatedPlanetIndices.Add( path[k].PlanetIndex );
                Context.QueueCommandForSendingAtEndOfContext( command );
            }

            return;
        }

        //sends a raid against a planet chosen at random from a List
        //Not currently used, but it may be helpful someday so hold onto it
        public static void Helper_RaidPlanetFromList( List<GameEntity> threatShipsNotAssignedElsewhere, Planet originPlanet, WorldSide worldSide, Galaxy galaxy, List<Planet> threatPlanets, bool IgnorePathCosts, ArcenLongTermPlanningContext Context )
        {

            Planet threatTarget = threatPlanets[Context.QualityRandom.Next( 0, threatPlanets.Count )];
            /* Set up the data to let us get from any planet to any other given planet */
            for ( int k = 0; k < galaxy.Planets.Count; k++ )
            {
                Planet otherPlanet = galaxy.Planets[k];
                otherPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom = null;
                otherPlanet.FactionPlanning_CheapestRaidPathToHereCost = FInt.Zero;
            }
            List<Planet> potentialAttackTargets = new List<Planet>();
            List<Planet> planetsToCheckInFlood = new List<Planet>();
            planetsToCheckInFlood.Add( originPlanet );
            originPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom = originPlanet;
            for ( int k = 0; k < planetsToCheckInFlood.Count; k++ )
            {
                Planet floodPlanet = planetsToCheckInFlood[k];
                floodPlanet.DoForLinkedNeighbors( delegate ( Planet neighbor )
                {
                    FInt totalCostFromOriginToNeighbor = floodPlanet.FactionPlanning_CheapestRaidPathToHereCost + 1;
                    if ( !potentialAttackTargets.Contains( neighbor ) )
                        potentialAttackTargets.Add( neighbor );
                    if ( neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom != null &&
                         neighbor.FactionPlanning_CheapestRaidPathToHereCost <= totalCostFromOriginToNeighbor )
                        return DelReturn.Continue;
                    neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom = floodPlanet;
                    neighbor.FactionPlanning_CheapestRaidPathToHereCost = totalCostFromOriginToNeighbor;
                    planetsToCheckInFlood.Add( neighbor );
                    return DelReturn.Continue;
                } );
            }


            List<Planet> path = new List<Planet>();
            Planet workingPlanet = threatTarget;
            while ( workingPlanet != originPlanet )
            {
                path.Insert( 0, workingPlanet );
                workingPlanet = workingPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom;
            }
            if ( path.Count > 0 )
            {
                GameCommand command = GameCommand.Create( GameCommandType.SetWormholePath );
                for ( int k = 0; k < threatShipsNotAssignedElsewhere.Count; k++ )
                    command.RelatedEntityIDs.Add( threatShipsNotAssignedElsewhere[k].PrimaryKeyID );
                for ( int k = 0; k < path.Count; k++ )
                    command.RelatedPlanetIndices.Add( path[k].PlanetIndex );
                Context.QueueCommandForSendingAtEndOfContext( command );
            }

            return;
        }
    }

    /* Used to do the actual zombification by the Nanocaust Guns from the XML */
    public class DeathEffect_Nanocaustation : IDeathEffectImplementation
    {
        //originally in DeathEffects/Zombification.cs
        public void HandleDeathWithEffectApplied( GameEntity Entity, int ThisDeathEffectDamageSustained, WorldSide SideThatDidTheKilling, WorldSide SideResponsibleForTheDeathEffect, ArcenSimContext Context )
        {
            if ( SideResponsibleForTheDeathEffect == null )
                return;
            if ( !Entity.GetMatches( EntityRollupType.MobileCombatants ) )
                return;

            //ISpecialFactionImplementation implementationToSearchFor = SpecialFaction_NanobotZombie.Instance;

            /* Some nanobot ships should spawn other things than the original ship. It would be cooler */
            WorldSide destinationSide = SideThatDidTheKilling;
            if ( destinationSide == null )
                return;
            CombatSide sideForNewEntity = Entity.Combat.GetSideForWorldSide( destinationSide );
            GameEntity zombie = GameEntity.CreateNew( sideForNewEntity, Entity.TypeData, Entity.WorldLocation, Context );
            zombie.EntitySpecificOrders.Behavior = EntityBehaviorType.Attacker;
        }
    }
}