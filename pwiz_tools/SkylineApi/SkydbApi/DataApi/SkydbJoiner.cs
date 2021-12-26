using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using SkydbApi.Orm;

namespace SkydbApi.DataApi
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
            CopyEntities<MsDataFile>();
            using (var insertScoreStatement = new InsertScoresStatement(Output.Connection))
            {
                insertScoreStatement.CopyAll(Output.Connection, _entityIdMap);
            }

            CopyEntities<SpectrumInfo>();
            CopyEntities<SpectrumList>();
            CopyEntities<Orm.ChromatogramData>();
            CopyEntities<ChromatogramGroup>();
            CopyEntities<TransitionChromatogram>();
            CopyEntities<CandidatePeakGroup>();
            CopyEntities<CandidatePeak>();
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
