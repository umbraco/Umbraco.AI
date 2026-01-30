#!/bin/bash
set -e

# Get repository root (parent of scripts folder)
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Find all Umbraco.Ai* folders at root
mapfile -t PRODUCTS < <(find "$REPO_ROOT" -maxdepth 1 -type d -name "Umbraco.Ai*" -printf "%f\n" | sort)

if [ ${#PRODUCTS[@]} -eq 0 ]; then
    echo "Error: No Umbraco.Ai* folders found in repository root" >&2
    exit 1
fi

# Initialize selection state (all unselected by default)
declare -A SELECTED
for product in "${PRODUCTS[@]}"; do
    SELECTED["$product"]=0
done

show_menu() {
    clear
    echo ""
    echo "=== Generate Release Manifest ==="
    echo ""
    echo "Select products to include in this release:"
    echo ""

    for i in "${!PRODUCTS[@]}"; do
        product="${PRODUCTS[$i]}"
        if [ "${SELECTED[$product]}" -eq 1 ]; then
            checkbox="[X]"
            printf "  %d. \033[32m%s %s\033[0m\n" $((i + 1)) "$checkbox" "$product"
        else
            checkbox="[ ]"
            printf "  %d. \033[90m%s %s\033[0m\n" $((i + 1)) "$checkbox" "$product"
        fi
    done

    echo ""
    echo "Commands:"
    echo "  [1-${#PRODUCTS[@]}] - Toggle selection"
    echo "  [a] - Select All"
    echo "  [n] - Select None"
    echo "  [g] - Generate manifest and exit"
    echo "  [q] - Quit without saving"
    echo ""
}

get_selected_count() {
    local count=0
    for product in "${PRODUCTS[@]}"; do
        [ "${SELECTED[$product]}" -eq 1 ] && ((count++))
    done
    echo "$count"
}

get_selected_products() {
    local result=()
    for product in "${PRODUCTS[@]}"; do
        [ "${SELECTED[$product]}" -eq 1 ] && result+=("$product")
    done
    echo "${result[@]}"
}

generate_manifest() {
    local selected_count=$(get_selected_count)

    if [ "$selected_count" -eq 0 ]; then
        echo ""
        echo "Error: No products selected. Press Enter to continue..."
        read -r
        return 1
    fi

    # Build JSON array
    local json="["
    local first=true
    for product in "${PRODUCTS[@]}"; do
        if [ "${SELECTED[$product]}" -eq 1 ]; then
            if [ "$first" = true ]; then
                first=false
            else
                json+=","
            fi
            json+=$'\n'"  \"$product\""
        fi
    done
    json+=$'\n'"]"

    # Write to file
    local manifest_path="$REPO_ROOT/release-manifest.json"
    echo "$json" > "$manifest_path"

    echo ""
    echo "Generated: $manifest_path"
    echo ""
    echo "Contents:"
    echo "$json"
    echo ""
    echo "Selected $selected_count product(s)"
}

# Main loop
while true; do
    show_menu

    selected_count=$(get_selected_count)
    if [ "$selected_count" -gt 0 ]; then
        printf "\033[32mSelected: %d product(s)\033[0m\n" "$selected_count"
    else
        printf "\033[31mSelected: None\033[0m\n"
    fi
    echo ""

    read -rp "Choose option: " choice

    case "${choice,,}" in
        [0-9]*)
            if [ "$choice" -ge 1 ] && [ "$choice" -le "${#PRODUCTS[@]}" ]; then
                product="${PRODUCTS[$((choice - 1))]}"
                if [ "${SELECTED[$product]}" -eq 1 ]; then
                    SELECTED["$product"]=0
                else
                    SELECTED["$product"]=1
                fi
            else
                echo "Invalid number. Press Enter to continue..."
                read -r
            fi
            ;;

        a)
            for product in "${PRODUCTS[@]}"; do
                SELECTED["$product"]=1
            done
            ;;

        n)
            for product in "${PRODUCTS[@]}"; do
                SELECTED["$product"]=0
            done
            ;;

        g)
            if generate_manifest; then
                exit 0
            fi
            ;;

        q)
            echo ""
            echo "Cancelled. No changes made."
            exit 0
            ;;

        *)
            echo "Invalid choice. Press Enter to continue..."
            read -r
            ;;
    esac
done
