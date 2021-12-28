using System.Data;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataApi
{
    public class SkydbJoiner
    {
        private EntityIdMap _entityIdMap = new EntityIdMap();
        private IDbConnection _input;
        public SkydbJoiner(SkydbWriter writer, IDbConnection input)
        {
            Output = writer;
            _input = input;
        }

        public SkydbWriter Output { get; }

        public void JoinFiles()
        {
            Output.EnsureScores(InsertScoresStatement.GetScoreNames(_input));
            CopyEntities<ExtractedFile>();
            using (var insertScoreStatement = new InsertScoresStatement(Output.Connection))
            {
                insertScoreStatement.CopyAll(Output.Connection, _entityIdMap);
            }

            CopyEntities<SpectrumInfo>();
            CopyEntities<SpectrumList>();
            CopyEntities<ChromatogramData>();
            CopyEntities<ChromatogramGroup>();
            CopyEntities<Chromatogram>();
            CopyEntities<CandidatePeakGroup>();
            CopyEntities<CandidatePeak>();
            CopyEntities<InstrumentInfo>();
        }

        private void CopyEntities<T>() where T : Entity
        {
            using (var insertStatement = new InsertStatement<T>(Output.Connection))
            {
                insertStatement.CopyAll(_input, _entityIdMap);
            }
        }
    }
}
