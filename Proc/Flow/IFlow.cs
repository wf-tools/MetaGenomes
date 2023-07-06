using System.Threading.Tasks;

namespace ShotgunMetagenome.Proc.Flow
{
    public interface IFlow
    {
        Task<string> CallFlowAsync();
        string StartFlow();
        string CancelFlow();

        string ExecuteMethod { set; get; }
        public static readonly string NormalEndMessage = "";
        public static readonly string CanceledMessage = " Cancel ";
        public static readonly string ErrorEndMessage = " Error ";

    }
}
