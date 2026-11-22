from PIL import Image, ImageDraw
from pathlib import Path
import math

out = Path("src/PIblox.App/Assets/PIblox.ico")
out.parent.mkdir(parents=True, exist_ok=True)
sizes=[16,24,32,48,64,128,256]
frames=[]
for size in sizes:
    im=Image.new("RGBA",(size,size),(0,0,0,0))
    d=ImageDraw.Draw(im)
    # Rounded dark glass base
    m=max(1,size//16)
    d.rounded_rectangle((m,m,size-m-1,size-m-1),radius=size//5,fill=(16,19,44,255),outline=(100,82,245,255),width=max(1,size//32))
    # glowing cube geometric mark
    cx=size/2; cy=size/2; r=size*.31
    top=(cx,cy-r)
    left=(cx-r*.87,cy-r*.5)
    right=(cx+r*.87,cy-r*.5)
    bottom=(cx,cy+r)
    d.polygon([top,left,(cx,cy)],fill=(147,105,255,255))
    d.polygon([top,right,(cx,cy)],fill=(80,128,255,255))
    d.polygon([left,bottom,right,(cx,cy)],fill=(55,66,180,255))
    d.line([top,left,bottom,right,top],fill=(220,215,255,255),width=max(1,size//28))
    d.line([left,(cx,cy),right],fill=(220,215,255,255),width=max(1,size//28))
    d.line([top,(cx,cy),bottom],fill=(220,215,255,255),width=max(1,size//28))
    frames.append(im)
frames[-1].save(out,format="ICO",sizes=[(s,s) for s in sizes],append_images=frames[:-1])
print("Generated",out)
