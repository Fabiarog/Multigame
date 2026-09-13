import runpy
from pathlib import Path
runpy.run_path(str(Path(__file__).with_name('refine_cast26_mcp.py')),init_globals={
    'CAST_IDS':['nina','bento','corvo','onca','iara','zeca','aki','barao','dama','morgana','carnical']})
