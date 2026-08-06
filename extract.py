import zipfile
import os

zip_path = '/home/azureuser/deploy_package.zip'
extract_dir = '/home/azureuser/app'

print(f"Extracting {zip_path} to {extract_dir}...")
os.makedirs(extract_dir, exist_ok=True)

with zipfile.ZipFile(zip_path, 'r') as z:
    for member in z.infolist():
        # Convert backslashes to forward slashes for Linux paths
        clean_name = member.filename.replace('\\', '/')
        target_path = os.path.normpath(os.path.join(extract_dir, clean_name))
        
        if member.is_dir() or clean_name.endswith('/'):
            os.makedirs(target_path, exist_ok=True)
        else:
            os.makedirs(os.path.dirname(target_path), exist_ok=True)
            with z.open(member) as source, open(target_path, 'wb') as target:
                target.write(source.read())

print("Extraction completed successfully!")
