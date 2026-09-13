import runpy
from pathlib import Path
runpy.run_path(str(Path(__file__).with_name('preview_cast26_mcp.py')),init_globals={
    'CAST_IDS':['onca','corvo','nina','bento','iara','zeca','aki','barao','dama','morgana','carnical']})
