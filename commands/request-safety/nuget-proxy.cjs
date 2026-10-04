// Windows sandbox schannel cannot restore HTTPS. This loopback-only, read-only
// proxy fetches NuGet via Node's verified OpenSSL TLS; it never disables validation.
const http = require('http');
const port = Number(process.env.REQUEST_SAFETY_NUGET_PORT || 18763);
http.createServer(async (request, response) => {
  if (request.method !== 'GET') { response.writeHead(405).end(); return; }
  try {
    if (request.url === '/request-safety-health') { response.writeHead(200).end('unity-mcp-request-safety-nuget-v1'); return; }
    if (request.url === '/v3/index.json') {
      response.writeHead(200, { 'Content-Type': 'application/json' });
      response.end(JSON.stringify({ version: '3.0.0', resources: [
        { '@id': `http://127.0.0.1:${port}/v3-flatcontainer/`, '@type': 'PackageBaseAddress/3.0.0' }
      ] }));
      return;
    }
    if (!request.url.startsWith('/v3-flatcontainer/')) { response.writeHead(404).end(); return; }
    const upstream = await fetch('https://api.nuget.org' + request.url);
    let body = Buffer.from(await upstream.arrayBuffer());
    if (request.url.split('?')[0].endsWith('.json'))
      body = Buffer.from(body.toString().replaceAll('https://api.nuget.org', `http://127.0.0.1:${port}`));
    response.writeHead(upstream.status, { 'Content-Type': upstream.headers.get('content-type') || 'application/octet-stream' });
    response.end(body);
  } catch (error) { response.writeHead(502).end(String(error)); }
}).listen(port, '127.0.0.1', () => console.log(`NuGet TLS proxy listening on 127.0.0.1:${port}`));
