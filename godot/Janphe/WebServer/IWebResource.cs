using System.Collections;


namespace Janphe
{
	public interface IWebResource
	{
        void HandleRequest(Request request, Response response);
	}

}
