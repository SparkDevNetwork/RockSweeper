using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Removes all interactive experience data.
    /// </summary>
    [ActionId( "dad894c1-933b-4072-84e3-f6ae3320aa74" )]
    [Title( "Interactive Experiences (Remove Data)" )]
    [Description( "Removes all interactive experience data." )]
    [Category( "Data Scrubbing" )]
    public class InteractiveExperienceRemoveData : SweeperAction
    {
        private readonly int _stepCount = 6;

        public override async Task ExecuteAsync()
        {
            await ProcessInteractiveExperienceAnswersAsync();
            await ProcessInteractiveExperienceOccurrencesAsync();
            await ProcessInteractiveExperienceScheduleCampusesAsync();
            await ProcessInteractiveExperienceSchedulesAsync();
            await ProcessInteractiveExperienceActionsAsync();
            await ProcessInteractiveExperiencesAsync();
        }

        private async Task ProcessInteractiveExperienceAnswersAsync()
        {
            var minId = ( await Sweeper.SqlScalarAsync<int?>( "SELECT MIN([Id]) FROM [InteractiveExperienceAnswer]" ) ?? 0 );
            var maxId = ( await Sweeper.SqlScalarAsync<int?>( "SELECT MAX([Id]) FROM [InteractiveExperienceAnswer]" ) ?? 0 );
            var idChunks = new List<IdChunk>();

            for ( int id = minId; id <= maxId; id += 25_000 )
            {
                idChunks.Add( new IdChunk
                {
                    First = id,
                    Last = Math.Min( id + 25_000 - 1, maxId )
                } );
            }

            var reporter = new CountProgressReporter( idChunks.Count, p => Progress( p, 1, _stepCount ) );

            await AsyncProducer.FromItems( idChunks )
                .Consume( async chunk =>
                {
                    await Sweeper.SqlCommandAsync( $"DELETE FROM [InteractiveExperienceAnswer] WHERE [Id] >= {chunk.First} AND [Id] <= {chunk.Last}" );

                    reporter.Add( 1 );
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task ProcessInteractiveExperienceOccurrencesAsync()
        {
            // Check if this Rock version supports Interactive Experience Occurrences.
            try
            {
                await Sweeper.SqlScalarAsync<int>( "SELECT COUNT(*) FROM [InteractiveExperienceOccurrence]" );
            }
            catch
            {
                Progress( 1, 2, _stepCount );
                return;
            }

            await Sweeper.SqlCommandAsync( "DELETE FROM [InteractiveExperienceOccurrence]" );
            Progress( 1, 2, _stepCount );
        }

        private async Task ProcessInteractiveExperienceScheduleCampusesAsync()
        {
            await Sweeper.SqlCommandAsync( "DELETE FROM [InteractiveExperienceScheduleCampus]" );
            Progress( 1, 3, _stepCount );
        }

        private async Task ProcessInteractiveExperienceSchedulesAsync()
        {
            await Sweeper.SqlCommandAsync( "DELETE FROM [InteractiveExperienceSchedule]" );
            Progress( 1, 4, _stepCount );
        }

        private async Task ProcessInteractiveExperienceActionsAsync()
        {
            await Sweeper.SqlCommandAsync( "DELETE FROM [InteractiveExperienceAction]" );
            Progress( 1, 5, _stepCount );
        }

        private async Task ProcessInteractiveExperiencesAsync()
        {
            await Sweeper.SqlCommandAsync( "DELETE FROM [InteractiveExperience]" );
            Progress( 1, 6, _stepCount );
        }

        private class IdChunk
        {
            public int First { get; set; }

            public int Last { get; set; }
        }
    }
}
