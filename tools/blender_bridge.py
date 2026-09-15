"""Local JSON transport used by the installed Blender MCP addon (port 9876).
No asset downloads, telemetry settings or external services are used here.
Usage: python tools/blender_bridge.py get_scene_info
       python tools/blender_bridge.py execute_code --code-file tools/example.py
"""
import argparse, json, socket
from pathlib import Path

def request(command, params=None, timeout=60):
    payload=json.dumps({'type':command,'params':params or {}}).encode('utf-8')
    with socket.create_connection(('127.0.0.1',9876),timeout=5) as connection:
        connection.settimeout(timeout); connection.sendall(payload)
        data=bytearray()
        while True:
            chunk=connection.recv(1024*1024)
            if not chunk: raise RuntimeError('Blender closed the connection before a complete response.')
            data.extend(chunk)
            try: response=json.loads(data.decode('utf-8'))
            except (json.JSONDecodeError,UnicodeDecodeError): continue
            if response.get('status')=='error': raise RuntimeError(response.get('message',str(response)))
            return response

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('command'); parser.add_argument('--code-file'); parser.add_argument('--params',default='{}'); parser.add_argument('--timeout',type=int,default=60)
    args=parser.parse_args(); params=json.loads(args.params)
    if args.code_file:
        path = Path(args.code_file).resolve()
        params['code'] = f'__file__ = {str(path)!r}\n' + path.read_text(encoding='utf-8')
    print(json.dumps(request(args.command,params,args.timeout),ensure_ascii=False,indent=2))
